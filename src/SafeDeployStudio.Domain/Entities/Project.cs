using SafeDeployStudio.Domain.Common;

namespace SafeDeployStudio.Domain.Entities;

public class Project : Entity, IAggregateRoot
{
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid? GuardrailPolicyId { get; set; }
    public GuardrailPolicy? GuardrailPolicy { get; set; }

    public List<EnvironmentDefinition> Environments { get; set; } = [];
    public List<DeploymentRoute> DeploymentRoutes { get; set; } = [];
    public List<DatabaseConnection> DatabaseConnections { get; set; } = [];
    public List<DeploymentRecord> DeploymentRecords { get; set; } = [];
    public List<BackupSnapshot> BackupSnapshots { get; set; } = [];

    public Project() { }

    public Project(Guid organizationId, string name, string slug, string description = "")
    {
        OrganizationId = organizationId;
        Name = name;
        Slug = slug.ToLowerInvariant().Trim();
        Description = description;
    }
}
