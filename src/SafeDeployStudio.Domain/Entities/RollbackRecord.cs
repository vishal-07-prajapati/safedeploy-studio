using SafeDeployStudio.Domain.Common;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.Domain.Entities;

public class RollbackRecord : Entity
{
    public Guid DeploymentRecordId { get; set; }
    public DeploymentRecord? DeploymentRecord { get; set; }

    public Guid BackupSnapshotId { get; set; }
    public BackupSnapshot? BackupSnapshot { get; set; }

    public string InitiatedBy { get; set; } = "developer";
    public DateTime InitiatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Draft;
    public string? ExecutionLog { get; set; }
    public string? ErrorMessage { get; set; }

    public RollbackRecord() { }

    public RollbackRecord(Guid deploymentRecordId, Guid backupSnapshotId, string initiatedBy = "developer")
    {
        DeploymentRecordId = deploymentRecordId;
        BackupSnapshotId = backupSnapshotId;
        InitiatedBy = initiatedBy;
        Status = DeploymentStatus.Deploying;
    }
}
