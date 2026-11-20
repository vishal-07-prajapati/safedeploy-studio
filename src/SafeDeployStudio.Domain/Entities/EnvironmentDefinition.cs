using SafeDeployStudio.Domain.Common;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.Domain.Entities;

public class EnvironmentDefinition : Entity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = string.Empty;
    public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.Development;
    public string DisplayColor { get; set; } = "#3B82F6";
    public int OrderIndex { get; set; } = 0;
    public bool IsProduction { get; set; } = false;

    public Guid? DatabaseConnectionId { get; set; }
    public DatabaseConnection? DatabaseConnection { get; set; }

    public EnvironmentDefinition() { }

    public EnvironmentDefinition(Guid projectId, string name, EnvironmentType environmentType, bool isProduction = false, string displayColor = "#3B82F6")
    {
        ProjectId = projectId;
        Name = name;
        EnvironmentType = environmentType;
        IsProduction = isProduction;
        DisplayColor = displayColor;
    }
}
