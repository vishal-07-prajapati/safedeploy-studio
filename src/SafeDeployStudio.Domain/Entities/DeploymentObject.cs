using SafeDeployStudio.Domain.Common;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.Domain.Entities;

public class DeploymentObject : Entity
{
    public Guid DeploymentRecordId { get; set; }
    public DeploymentRecord? DeploymentRecord { get; set; }

    public string SchemaName { get; set; } = "dbo";
    public string ObjectName { get; set; } = string.Empty;
    public DatabaseObjectType ObjectType { get; set; } = DatabaseObjectType.StoredProcedure;

    public string SourceScript { get; set; } = string.Empty;
    public string? TargetScriptBefore { get; set; }
    public string? TargetScriptAfter { get; set; }

    public string? DiffSummary { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public string? SargabilityWarningsJson { get; set; }
    public string? GuardrailViolationsJson { get; set; }
    public int OrderIndex { get; set; } = 0;

    public DeploymentObject() { }

    public DeploymentObject(Guid deploymentRecordId, string schemaName, string objectName, DatabaseObjectType objectType, string sourceScript)
    {
        DeploymentRecordId = deploymentRecordId;
        SchemaName = schemaName;
        ObjectName = objectName;
        ObjectType = objectType;
        SourceScript = sourceScript;
    }
}
