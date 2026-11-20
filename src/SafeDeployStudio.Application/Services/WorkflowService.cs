using SafeDeployStudio.Application.DTOs;
using SafeDeployStudio.Backup;
using SafeDeployStudio.DatabaseProviders.Abstractions;
using SafeDeployStudio.DatabaseProviders.Abstractions.Models;
using SafeDeployStudio.Deployment;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.Repositories;
using SafeDeployStudio.Domain.ValueObjects;
using SafeDeployStudio.Guardrails;

namespace SafeDeployStudio.Application.Services;

public interface IWorkflowService
{
    Task<ProcedureCompareResponse> CompareProcedureAsync(
        ProcedureCompareRequest request,
        CancellationToken ct = default);

    Task<DeploymentRecord> ExecuteDeploymentAsync(
        ExecuteDeploymentCommand command,
        Action<string>? logCallback = null,
        CancellationToken ct = default);

    Task<RollbackRecord> ExecuteRollbackAsync(
        RollbackCommand command,
        CancellationToken ct = default);

    Task<IReadOnlyList<SchemaObjectInfo>> ListProceduresAsync(
        Guid environmentId,
        CancellationToken ct = default);
}

public class WorkflowService : IWorkflowService
{
    private readonly IProjectRepository _projectRepo;
    private readonly IDatabaseConnectionRepository _connRepo;
    private readonly IDatabaseProviderFactory _providerFactory;
    private readonly ISqlDiffEngine _diffEngine;
    private readonly IGuardrailEngine _guardrailEngine;
    private readonly IGuardrailPolicyRepository _guardrailRepo;
    private readonly IDeploymentService _deploymentService;
    private readonly IBackupVaultService _backupVault;

    public WorkflowService(
        IProjectRepository projectRepo,
        IDatabaseConnectionRepository connRepo,
        IDatabaseProviderFactory providerFactory,
        ISqlDiffEngine diffEngine,
        IGuardrailEngine guardrailEngine,
        IGuardrailPolicyRepository guardrailRepo,
        IDeploymentService deploymentService,
        IBackupVaultService backupVault)
    {
        _projectRepo = projectRepo;
        _connRepo = connRepo;
        _providerFactory = providerFactory;
        _diffEngine = diffEngine;
        _guardrailEngine = guardrailEngine;
        _guardrailRepo = guardrailRepo;
        _deploymentService = deploymentService;
        _backupVault = backupVault;
    }

    public async Task<ProcedureCompareResponse> CompareProcedureAsync(
        ProcedureCompareRequest request,
        CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(request.ProjectId, ct)
                      ?? throw new KeyNotFoundException($"Project '{request.ProjectId}' was not found.");

        var sourceEnv = project.Environments.FirstOrDefault(e => e.Id == request.SourceEnvironmentId)
                        ?? throw new KeyNotFoundException($"Source environment '{request.SourceEnvironmentId}' was not found.");

        var targetEnv = project.Environments.FirstOrDefault(e => e.Id == request.TargetEnvironmentId)
                        ?? throw new KeyNotFoundException($"Target environment '{request.TargetEnvironmentId}' was not found.");

        if (sourceEnv.DatabaseConnectionId == null || targetEnv.DatabaseConnectionId == null)
            throw new InvalidOperationException("Both source and target environments must have an active database connection configured.");

        var sourceConn = await _connRepo.GetByIdAsync(sourceEnv.DatabaseConnectionId.Value, ct)
                         ?? throw new KeyNotFoundException("Source database connection was not found.");
        var targetConn = await _connRepo.GetByIdAsync(targetEnv.DatabaseConnectionId.Value, ct)
                         ?? throw new KeyNotFoundException("Target database connection was not found.");

        var sourceProvider = _providerFactory.GetProvider(sourceConn.ProviderType);
        var targetProvider = _providerFactory.GetProvider(targetConn.ProviderType);

        var sourceDef = await sourceProvider.GetObjectDefinitionAsync(
            sourceConn, request.SchemaName, request.ProcedureName, DatabaseObjectType.StoredProcedure, ct);

        var targetDef = await targetProvider.GetObjectDefinitionAsync(
            targetConn, request.SchemaName, request.ProcedureName, DatabaseObjectType.StoredProcedure, ct);

        var diff = _diffEngine.CompareSql(
            targetDef,
            sourceDef,
            objectName: request.ProcedureName,
            schemaName: request.SchemaName);

        var policy = await _guardrailRepo.GetByProjectIdAsync(project.Id, ct)
                     ?? new GuardrailPolicy(project.Id, "Default Policy");

        var guardrailResult = await _guardrailEngine.ValidateScriptAsync(sourceDef, policy, ct);

        return new ProcedureCompareResponse(
            request.SchemaName,
            request.ProcedureName,
            sourceDef,
            targetDef,
            diff,
            guardrailResult
        );
    }

    public async Task<DeploymentRecord> ExecuteDeploymentAsync(
        ExecuteDeploymentCommand command,
        Action<string>? logCallback = null,
        CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(command.ProjectId, ct)
                      ?? throw new KeyNotFoundException($"Project '{command.ProjectId}' was not found.");

        var sourceEnv = project.Environments.FirstOrDefault(e => e.Id == command.SourceEnvironmentId)
                        ?? throw new KeyNotFoundException($"Source environment '{command.SourceEnvironmentId}' was not found.");

        var targetEnv = project.Environments.FirstOrDefault(e => e.Id == command.TargetEnvironmentId)
                        ?? throw new KeyNotFoundException($"Target environment '{command.TargetEnvironmentId}' was not found.");

        if (targetEnv.DatabaseConnectionId == null)
            throw new InvalidOperationException("Target environment does not have an active database connection configured.");

        var targetConn = await _connRepo.GetByIdAsync(targetEnv.DatabaseConnectionId.Value, ct)
                         ?? throw new KeyNotFoundException("Target database connection was not found.");

        var deploymentObjects = command.Objects.Select((o, idx) => new DeploymentObject
        {
            SchemaName = o.SchemaName,
            ObjectName = o.ObjectName,
            ObjectType = o.ObjectType,
            SourceScript = o.Script,
            OrderIndex = idx
        }).ToList();

        return await _deploymentService.ExecuteDeploymentAsync(
            project,
            sourceEnv,
            targetEnv,
            targetConn,
            deploymentObjects,
            command.Title,
            command.CreatedBy,
            logCallback,
            ct);
    }

    public async Task<RollbackRecord> ExecuteRollbackAsync(
        RollbackCommand command,
        CancellationToken ct = default)
    {
        var conn = await _connRepo.GetByIdAsync(command.TargetEnvironmentId, ct)
                   ?? throw new KeyNotFoundException($"Database connection for target environment '{command.TargetEnvironmentId}' was not found.");

        return await _backupVault.ExecuteRollbackAsync(
            command.BackupSnapshotId,
            conn,
            command.InitiatedBy,
            ct);
    }

    public async Task<IReadOnlyList<SchemaObjectInfo>> ListProceduresAsync(
        Guid environmentId,
        CancellationToken ct = default)
    {
        var conn = await _connRepo.GetByIdAsync(environmentId, ct);
        if (conn == null)
            return [];

        var provider = _providerFactory.GetProvider(conn.ProviderType);
        return await provider.ListObjectsAsync(conn, DatabaseObjectType.StoredProcedure, ct);
    }
}
