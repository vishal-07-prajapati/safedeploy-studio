using SafeDeployStudio.Domain.Common;

namespace SafeDeployStudio.Domain.Entities;

public class DeploymentRoute : Entity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = string.Empty;
    public Guid SourceEnvironmentId { get; set; }
    public EnvironmentDefinition? SourceEnvironment { get; set; }

    public Guid TargetEnvironmentId { get; set; }
    public EnvironmentDefinition? TargetEnvironment { get; set; }

    public bool RequireDryRunPass { get; set; } = true;
    public bool RequireApprovalForProduction { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public DeploymentRoute() { }

    public DeploymentRoute(Guid projectId, string name, Guid sourceEnvironmentId, Guid targetEnvironmentId)
    {
        ProjectId = projectId;
        Name = name;
        SourceEnvironmentId = sourceEnvironmentId;
        TargetEnvironmentId = targetEnvironmentId;
    }
}
