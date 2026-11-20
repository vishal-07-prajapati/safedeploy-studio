using SafeDeployStudio.Domain.Common;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.Domain.Entities;

public class DeploymentRecord : Entity, IAggregateRoot
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? RouteId { get; set; }
    public DeploymentRoute? Route { get; set; }

    public Guid SourceEnvironmentId { get; set; }
    public EnvironmentDefinition? SourceEnvironment { get; set; }

    public Guid TargetEnvironmentId { get; set; }
    public EnvironmentDefinition? TargetEnvironment { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Draft;
    public RiskLevel OverallRiskLevel { get; set; } = RiskLevel.Low;

    public string CreatedBy { get; set; } = "system";
    public DateTime? ExecutedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long ExecutionDurationMs { get; set; }
    public string? ExecutionLog { get; set; }
    public string? ErrorMessage { get; set; }

    public Guid? BackupSnapshotId { get; set; }
    public BackupSnapshot? BackupSnapshot { get; set; }

    public Guid? RollbackRecordId { get; set; }
    public RollbackRecord? RollbackRecord { get; set; }

    public List<DeploymentObject> Objects { get; set; } = [];

    public DeploymentRecord() { }

    public DeploymentRecord(Guid projectId, Guid sourceEnvId, Guid targetEnvId, string title, string createdBy = "developer")
    {
        ProjectId = projectId;
        SourceEnvironmentId = sourceEnvId;
        TargetEnvironmentId = targetEnvId;
        Title = title;
        CreatedBy = createdBy;
        Status = DeploymentStatus.Draft;
    }
}
