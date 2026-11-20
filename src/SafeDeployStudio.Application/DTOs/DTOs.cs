using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Application.DTOs;

public record OrganizationDto(Guid Id, string Name, string Slug, string Description, int ProjectCount);
public record CreateOrganizationRequest(string Name, string Slug, string Description = "");

public record ProjectSummaryDto(Guid Id, Guid OrganizationId, string Name, string Slug, string Description, int EnvironmentCount, int DeploymentCount);
public record CreateProjectRequest(Guid OrganizationId, string Name, string Slug, string Description = "");

public record EnvironmentDto(Guid Id, Guid ProjectId, string Name, EnvironmentType EnvironmentType, string DisplayColor, bool IsProduction, Guid? DatabaseConnectionId);
public record CreateEnvironmentRequest(Guid ProjectId, string Name, EnvironmentType EnvironmentType, bool IsProduction, string DisplayColor = "#3B82F6", Guid? ConnectionId = null);

public record DatabaseConnectionDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    DatabaseProviderType ProviderType,
    string Server,
    int Port,
    string DatabaseName,
    AuthenticationType AuthType,
    string? Username,
    bool TrustServerCertificate,
    bool UseTlsSsl,
    int TimeoutSeconds
);

public record CreateDatabaseConnectionRequest(
    Guid ProjectId,
    string Name,
    DatabaseProviderType ProviderType,
    string Server,
    string DatabaseName,
    int Port = 1433,
    AuthenticationType AuthType = AuthenticationType.WindowsIntegrated,
    string? Username = null,
    string? Password = null,
    bool TrustServerCertificate = true,
    bool UseTlsSsl = true,
    int TimeoutSeconds = 30
);

public record ProcedureCompareRequest(
    Guid ProjectId,
    Guid SourceEnvironmentId,
    Guid TargetEnvironmentId,
    string SchemaName,
    string ProcedureName
);

public record ProcedureCompareResponse(
    string SchemaName,
    string ProcedureName,
    string SourceDefinition,
    string TargetDefinition,
    SqlDiffResult DiffResult,
    GuardrailResult GuardrailResult
);

public record ExecuteDeploymentCommand(
    Guid ProjectId,
    Guid SourceEnvironmentId,
    Guid TargetEnvironmentId,
    string Title,
    List<DeploymentObjectItem> Objects,
    string CreatedBy = "developer"
);

public record DeploymentObjectItem(
    string SchemaName,
    string ObjectName,
    DatabaseObjectType ObjectType,
    string Script
);

public record DeploymentRecordDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string SourceEnvironmentName,
    string TargetEnvironmentName,
    DeploymentStatus Status,
    RiskLevel OverallRiskLevel,
    string CreatedBy,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    long ExecutionDurationMs,
    string? ErrorMessage,
    Guid? BackupSnapshotId,
    int ObjectCount
);

public record RollbackCommand(
    Guid BackupSnapshotId,
    Guid TargetEnvironmentId,
    string InitiatedBy = "developer"
);
