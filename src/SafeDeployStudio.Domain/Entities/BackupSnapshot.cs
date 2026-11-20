using SafeDeployStudio.Domain.Common;

namespace SafeDeployStudio.Domain.Entities;

public class BackupSnapshot : Entity, IAggregateRoot
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid TargetEnvironmentId { get; set; }
    public EnvironmentDefinition? TargetEnvironment { get; set; }

    public Guid? DeploymentRecordId { get; set; }
    public DeploymentRecord? DeploymentRecord { get; set; }

    public string SnapshotName { get; set; } = string.Empty;
    public string FileStoragePath { get; set; } = string.Empty;
    public string RawScriptPayload { get; set; } = string.Empty;
    public string ObjectNamesJson { get; set; } = "[]";
    public string Sha256Checksum { get; set; } = string.Empty;
    public bool IsRestorable { get; set; } = true;

    public BackupSnapshot() { }

    public BackupSnapshot(Guid projectId, Guid targetEnvId, string snapshotName, string rawScriptPayload, string sha256Checksum)
    {
        ProjectId = projectId;
        TargetEnvironmentId = targetEnvId;
        SnapshotName = snapshotName;
        RawScriptPayload = rawScriptPayload;
        Sha256Checksum = sha256Checksum;
    }
}
