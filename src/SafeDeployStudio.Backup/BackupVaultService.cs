using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafeDeployStudio.DatabaseProviders.Abstractions;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.Repositories;

namespace SafeDeployStudio.Backup;

public interface IBackupVaultService
{
    Task<BackupSnapshot> CreatePreChangeSnapshotAsync(
        Project project,
        EnvironmentDefinition targetEnv,
        DatabaseConnection connection,
        IReadOnlyList<DeploymentObject> objects,
        Guid? deploymentId = null,
        CancellationToken ct = default);

    Task<RollbackRecord> ExecuteRollbackAsync(
        Guid backupSnapshotId,
        DatabaseConnection connection,
        string initiatedBy = "developer",
        CancellationToken ct = default);

    Task<string> ReadSnapshotFileAsync(BackupSnapshot snapshot, CancellationToken ct = default);
}

public class BackupVaultService : IBackupVaultService
{
    private readonly IDatabaseProviderFactory _providerFactory;
    private readonly IBackupSnapshotRepository _snapshotRepo;
    private readonly string _storageRoot;

    public BackupVaultService(
        IDatabaseProviderFactory providerFactory,
        IBackupSnapshotRepository snapshotRepo,
        string? storageRoot = null)
    {
        _providerFactory = providerFactory;
        _snapshotRepo = snapshotRepo;
        _storageRoot = storageRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SafeDeployStudio", "Vault");
    }

    public async Task<BackupSnapshot> CreatePreChangeSnapshotAsync(
        Project project,
        EnvironmentDefinition targetEnv,
        DatabaseConnection connection,
        IReadOnlyList<DeploymentObject> objects,
        Guid? deploymentId = null,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider(connection.ProviderType);
        var sb = new StringBuilder();
        var objectNames = new List<string>();

        sb.AppendLine($"-- ===========================================================================");
        sb.AppendLine($"-- SAFEDEPLOY STUDIO PRE-CHANGE BACKUP SNAPSHOT");
        sb.AppendLine($"-- Project: {project.Name} ({project.Slug})");
        sb.AppendLine($"-- Target Environment: {targetEnv.Name} ({targetEnv.EnvironmentType})");
        sb.AppendLine($"-- Created At (UTC): {DateTime.UtcNow:u}");
        sb.AppendLine($"-- Deployment Record: {deploymentId?.ToString() ?? "Ad-hoc"}");
        sb.AppendLine($"-- ===========================================================================");
        sb.AppendLine();

        foreach (var obj in objects)
        {
            ct.ThrowIfCancellationRequested();
            objectNames.Add($"{obj.SchemaName}.{obj.ObjectName}");

            var existingDef = await provider.GetObjectDefinitionAsync(
                connection, obj.SchemaName, obj.ObjectName, obj.ObjectType, ct);

            obj.TargetScriptBefore = existingDef;

            sb.AppendLine($"-- ---------------------------------------------------------------------------");
            sb.AppendLine($"-- Object: [{obj.SchemaName}].[{obj.ObjectName}] ({obj.ObjectType})");
            sb.AppendLine($"-- ---------------------------------------------------------------------------");

            // Always add DROP IF EXISTS for clean idempotent restoration
            sb.AppendLine(provider.GenerateDropIfExistsScript(obj.SchemaName, obj.ObjectName, obj.ObjectType));

            if (!string.IsNullOrWhiteSpace(existingDef))
            {
                sb.AppendLine(existingDef.Trim());
                sb.AppendLine("\nGO\n");
            }
            else
            {
                sb.AppendLine($"-- Note: Object did not exist on target database before migration.");
                sb.AppendLine();
            }
        }

        var fullPayload = sb.ToString();
        var sha256 = ComputeSha256(fullPayload);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var snapshotName = $"Snapshot_{project.Slug}_{targetEnv.Name}_{timestamp}";
        var folder = Path.Combine(_storageRoot, project.Slug, targetEnv.Name);
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, $"{snapshotName}.sql");
        await File.WriteAllTextAsync(filePath, fullPayload, Encoding.UTF8, ct);

        var snapshot = new BackupSnapshot(
            project.Id,
            targetEnv.Id,
            snapshotName,
            fullPayload,
            sha256)
        {
            DeploymentRecordId = deploymentId,
            FileStoragePath = filePath,
            ObjectNamesJson = JsonSerializer.Serialize(objectNames),
            IsRestorable = true
        };

        await _snapshotRepo.AddAsync(snapshot, ct);
        return snapshot;
    }

    public async Task<RollbackRecord> ExecuteRollbackAsync(
        Guid backupSnapshotId,
        DatabaseConnection connection,
        string initiatedBy = "developer",
        CancellationToken ct = default)
    {
        var snapshot = await _snapshotRepo.GetByIdAsync(backupSnapshotId, ct);
        if (snapshot == null)
            throw new KeyNotFoundException($"Backup snapshot with ID '{backupSnapshotId}' was not found.");

        var rollbackRecord = new RollbackRecord(snapshot.DeploymentRecordId ?? Guid.Empty, snapshot.Id, initiatedBy);
        var provider = _providerFactory.GetProvider(connection.ProviderType);

        var scriptPayload = snapshot.RawScriptPayload;
        if (string.IsNullOrWhiteSpace(scriptPayload) && File.Exists(snapshot.FileStoragePath))
        {
            scriptPayload = await File.ReadAllTextAsync(snapshot.FileStoragePath, ct);
        }

        var logBuilder = new StringBuilder();
        var batchResult = await provider.ExecuteMigrationScriptAsync(
            connection,
            scriptPayload,
            logMsg => logBuilder.AppendLine(logMsg),
            timeoutSeconds: 300,
            ct: ct);

        rollbackRecord.ExecutionLog = logBuilder.ToString();
        rollbackRecord.CompletedAtUtc = DateTime.UtcNow;

        if (batchResult.IsSuccessful)
        {
            rollbackRecord.Status = DeploymentStatus.RolledBack;
        }
        else
        {
            rollbackRecord.Status = DeploymentStatus.Failed;
            rollbackRecord.ErrorMessage = batchResult.ErrorMessage;
        }

        return rollbackRecord;
    }

    public async Task<string> ReadSnapshotFileAsync(BackupSnapshot snapshot, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.RawScriptPayload))
            return snapshot.RawScriptPayload;

        if (File.Exists(snapshot.FileStoragePath))
            return await File.ReadAllTextAsync(snapshot.FileStoragePath, ct);

        return string.Empty;
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexStringLower(bytes);
    }
}
