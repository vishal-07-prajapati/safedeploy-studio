using SafeDeployStudio.Domain.Common;

namespace SafeDeployStudio.Domain.Entities;

public class Organization : Entity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<Project> Projects { get; set; } = [];

    public Organization() { }

    public Organization(string name, string slug, string description = "")
    {
        Name = name;
        Slug = slug.ToLowerInvariant().Trim();
        Description = description;
    }
}
