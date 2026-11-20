using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Infrastructure.Data;
using Xunit;

namespace SafeDeployStudio.IntegrationTests;

public class DatabaseSeederIntegrationTests
{
    [Fact]
    public async Task SeedAsync_WhenDatabaseIsEmpty_ShouldSeedContosoOrganizationAndProjects()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SafeDeployDbContext>()
            .UseSqlite($"Data Source=file:memdb_{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;

        await using var db = new SafeDeployDbContext(options);
        await db.Database.OpenConnectionAsync();

        // Act
        await DatabaseSeeder.SeedAsync(db);

        // Assert
        var orgs = await db.Organizations.ToListAsync();
        orgs.Should().ContainSingle(o => o.Slug == "contoso");

        var projects = await db.Projects.Include(p => p.Environments).ToListAsync();
        projects.Should().ContainSingle(p => p.Slug == "platform-db");

        var envs = projects[0].Environments;
        envs.Should().HaveCount(3);
        envs.Should().Contain(e => e.IsProduction);
    }
}
