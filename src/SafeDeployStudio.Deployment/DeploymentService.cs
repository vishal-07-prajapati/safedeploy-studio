using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SafeDeployStudio.Backup;
using SafeDeployStudio.DatabaseProviders.Abstractions;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.Repositories;
using SafeDeployStudio.Domain.ValueObjects;
using SafeDeployStudio.Guardrails;

namespace SafeDeployStudio.Deployment;

public interface IDeploymentService
{
    Task<GuardrailResult> ValidateObjectsAsync(
        Project project,
        IReadOnlyList<DeploymentObject> objects,
        CancellationToken ct = default);

    Task<DryRunExecutionResult> RunSandboxDryRunAsync(
        DatabaseConnection targetConnection,
        IReadOnlyList<DeploymentObject> objects,
        CancellationToken ct = default);

    Task<DeploymentRecord> ExecuteDeploymentAsync(
        Project project,
        EnvironmentDefinition sourceEnv,
        EnvironmentDefinition targetEnv,
        DatabaseConnection targetConnection,
        IReadOnlyList<DeploymentObject> objects,
        string title,
        string createdBy = "developer",
        Action<string>? logCallback = null,
        CancellationToken ct = default);
}

public class DeploymentService : IDeploymentService
{
    private readonly IDatabaseProviderFactory _providerFactory;
    private readonly IGuardrailEngine _guardrailEngine;
    private readonly IBackupVaultService _backupVaultService;
    private readonly IDeploymentRecordRepository _deploymentRepo;
    private readonly IGuardrailPolicyRepository _guardrailRepo;

    public DeploymentService(
        IDatabaseProviderFactory providerFactory,
        IGuardrailEngine guardrailEngine,
        IBackupVaultService backupVaultService,
        IDeploymentRecordRepository deploymentRepo,
        IGuardrailPolicyRepository guardrailRepo)
    {
        _providerFactory = providerFactory;
        _guardrailEngine = guardrailEngine;
        _backupVaultService = backupVaultService;
        _deploymentRepo = deploymentRepo;
        _guardrailRepo = guardrailRepo;
    }

    public async Task<GuardrailResult> ValidateObjectsAsync(
        Project project,
        IReadOnlyList<DeploymentObject> objects,
        CancellationToken ct = default)
    {
        var policy = await _guardrailRepo.GetByProjectIdAsync(project.Id, ct)
                     ?? new GuardrailPolicy(project.Id, "Default SafeGuard Policy");

        var combinedResult = new GuardrailResult();

        foreach (var obj in objects)
        {
            var res = await _guardrailEngine.ValidateScriptAsync(obj.SourceScript, policy, ct);
            obj.RiskLevel = res.EstimatedRisk;
            obj.GuardrailViolationsJson = JsonSerializer.Serialize(res.Violations);
            combinedResult.Violations.AddRange(res.Violations);
        }

        if (combinedResult.Violations.Any(v => v.Severity == GuardrailSeverity.Critical))
            combinedResult.EstimatedRisk = RiskLevel.Critical;
        else if (combinedResult.Violations.Any(v => v.Severity == GuardrailSeverity.Error))
            combinedResult.EstimatedRisk = RiskLevel.High;
        else if (combinedResult.Violations.Any(v => v.Severity == GuardrailSeverity.Warning))
            combinedResult.EstimatedRisk = RiskLevel.Medium;
        else
            combinedResult.EstimatedRisk = RiskLevel.Low;

        return combinedResult;
    }

    public async Task<DryRunExecutionResult> RunSandboxDryRunAsync(
        DatabaseConnection targetConnection,
        IReadOnlyList<DeploymentObject> objects,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider(targetConnection.ProviderType);
        var sb = new StringBuilder();

        foreach (var obj in objects)
        {
            sb.AppendLine(obj.SourceScript);
            sb.AppendLine("\nGO\n");
        }

        return await provider.ExecuteDryRunAsync(targetConnection, sb.ToString(), timeoutSeconds: 60, ct);
    }

    public async Task<DeploymentRecord> ExecuteDeploymentAsync(
        Project project,
        EnvironmentDefinition sourceEnv,
        EnvironmentDefinition targetEnv,
        DatabaseConnection targetConnection,
        IReadOnlyList<DeploymentObject> objects,
        string title,
        string createdBy = "developer",
        Action<string>? logCallback = null,
        CancellationToken ct = default)
    {
        var record = new DeploymentRecord(project.Id, sourceEnv.Id, targetEnv.Id, title, createdBy)
        {
            Status = DeploymentStatus.Deploying,
            ExecutedAtUtc = DateTime.UtcNow
        };

        foreach (var obj in objects)
        {
            record.Objects.Add(obj);
        }

        await _deploymentRepo.AddAsync(record, ct);

        var sw = Stopwatch.StartNew();
        var logs = new StringBuilder();

        void Log(string msg)
        {
            var formatted = $"[{DateTime.UtcNow:HH:mm:ss}] {msg}";
            logs.AppendLine(formatted);
            logCallback?.Invoke(formatted);
        }

        try
        {
            Log($"Initiating deployment '{title}' to {targetEnv.Name} ({targetEnv.EnvironmentType})...");

            // 1. Guardrail Validation Gate
            Log("Step 1/4: Running safety guardrails validation...");
            var guardResult = await ValidateObjectsAsync(project, objects, ct);
            record.OverallRiskLevel = guardResult.EstimatedRisk;

            if (!guardResult.IsValid)
            {
                var criticals = guardResult.Violations
                    .Where(v => v.Severity == GuardrailSeverity.Error || v.Severity == GuardrailSeverity.Critical)
                    .Select(v => $"Line {v.LineNumber}: {v.Message}")
                    .ToList();

                var failMsg = $"Guardrail check failed with {criticals.Count} blocking violation(s):\n" + string.Join("\n", criticals);
                Log($"[ABORTED] {failMsg}");
                record.Status = DeploymentStatus.ValidationFailed;
                record.ErrorMessage = failMsg;
                record.ExecutionLog = logs.ToString();
                await _deploymentRepo.UpdateAsync(record, ct);
                return record;
            }
            Log("Guardrail validation passed successfully.");

            // 2. Transactional Dry-Run Verification
            Log("Step 2/4: Executing transactional dry-run on target sandbox...");
            var dryRun = await RunSandboxDryRunAsync(targetConnection, objects, ct);
            if (!dryRun.IsSuccessful)
            {
                var failMsg = $"Target sandbox dry-run execution failed: {dryRun.ErrorMessage}";
                Log($"[ABORTED] {failMsg}");
                record.Status = DeploymentStatus.DryRunFailed;
                record.ErrorMessage = failMsg;
                record.ExecutionLog = logs.ToString();
                await _deploymentRepo.UpdateAsync(record, ct);
                return record;
            }
            Log("Target sandbox dry-run passed cleanly with verified rollback.");

            // 3. Pre-Change Backup Snapshot
            Log("Step 3/4: Capturing pre-change backup snapshot in vault...");
            var snapshot = await _backupVaultService.CreatePreChangeSnapshotAsync(
                project, targetEnv, targetConnection, objects, record.Id, ct);
            record.BackupSnapshotId = snapshot.Id;
            Log($"Backup snapshot captured: {snapshot.SnapshotName} (SHA256: {snapshot.Sha256Checksum[..8]}...)");

            // 4. Apply Migration
            Log("Step 4/4: Applying changes to target database...");
            var provider = _providerFactory.GetProvider(targetConnection.ProviderType);
            var sb = new StringBuilder();
            foreach (var obj in objects)
            {
                sb.AppendLine(obj.SourceScript);
                sb.AppendLine("\nGO\n");
            }

            var batchResult = await provider.ExecuteMigrationScriptAsync(
                targetConnection,
                sb.ToString(),
                msg => Log(msg),
                timeoutSeconds: 300,
                ct: ct);

            if (batchResult.IsSuccessful)
            {
                record.Status = DeploymentStatus.Succeeded;
                Log($"[SUCCESS] Deployment applied successfully! {batchResult.BatchesExecuted} batches executed.");
            }
            else
            {
                record.Status = DeploymentStatus.Failed;
                record.ErrorMessage = batchResult.ErrorMessage;
                Log($"[FAILED] Deployment failed: {batchResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            record.Status = DeploymentStatus.Failed;
            record.ErrorMessage = ex.Message;
            Log($"[EXCEPTION] Unexpected deployment exception: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            record.ExecutionDurationMs = sw.ElapsedMilliseconds;
            record.CompletedAtUtc = DateTime.UtcNow;
            record.ExecutionLog = logs.ToString();
            await _deploymentRepo.UpdateAsync(record, ct);
        }

        return record;
    }
}
