using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(SafeDeployDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (db.Organizations.Any())
            return;

        var defaultOrg = new Organization("Contoso Enterprise", "contoso", "Sample multi-tenant enterprise organization.");
        await db.Organizations.AddAsync(defaultOrg);
        await db.SaveChangesAsync();

        var defaultProject = new Project(defaultOrg.Id, "Platform Database", "platform-db", "Primary enterprise transactional database.");
        await db.Projects.AddAsync(defaultProject);
        await db.SaveChangesAsync();

        var policy = new GuardrailPolicy(defaultProject.Id, "Standard Enterprise Guardrails")
        {
            BlockDropTable = true,
            BlockTruncateTable = true,
            BlockAlterPhysicalTable = true,
            AllowTempTablesOnly = true,
            RequireAsciiOnly = true,
            RequireWhereOnUpdateDelete = true
        };
        await db.GuardrailPolicies.AddAsync(policy);

        var devConn = new DatabaseConnection(defaultProject.Id, "Local Dev SQL Server", "localhost", "Platform_DEV", DatabaseProviderType.SqlServer)
        {
            AuthType = AuthenticationType.WindowsIntegrated
        };
        var stageConn = new DatabaseConnection(defaultProject.Id, "Staging SQL Server", "localhost", "Platform_STAGE", DatabaseProviderType.SqlServer)
        {
            AuthType = AuthenticationType.WindowsIntegrated
        };
        var prodConn = new DatabaseConnection(defaultProject.Id, "Production SQL Server", "localhost", "Platform_PROD", DatabaseProviderType.SqlServer)
        {
            AuthType = AuthenticationType.WindowsIntegrated
        };

        await db.DatabaseConnections.AddRangeAsync(devConn, stageConn, prodConn);
        await db.SaveChangesAsync();

        var devEnv = new EnvironmentDefinition(defaultProject.Id, "Development", EnvironmentType.Development, isProduction: false, displayColor: "#3B82F6")
        {
            DatabaseConnectionId = devConn.Id,
            OrderIndex = 1
        };
        var stageEnv = new EnvironmentDefinition(defaultProject.Id, "Staging", EnvironmentType.Staging, isProduction: false, displayColor: "#F59E0B")
        {
            DatabaseConnectionId = stageConn.Id,
            OrderIndex = 2
        };
        var prodEnv = new EnvironmentDefinition(defaultProject.Id, "Production", EnvironmentType.Production, isProduction: true, displayColor: "#EF4444")
        {
            DatabaseConnectionId = prodConn.Id,
            OrderIndex = 3
        };

        await db.Environments.AddRangeAsync(devEnv, stageEnv, prodEnv);
        await db.SaveChangesAsync();

        var routeDevStage = new DeploymentRoute(defaultProject.Id, "Dev to Staging", devEnv.Id, stageEnv.Id)
        {
            RequireDryRunPass = true,
            RequireApprovalForProduction = false
        };
        var routeStageProd = new DeploymentRoute(defaultProject.Id, "Staging to Production", stageEnv.Id, prodEnv.Id)
        {
            RequireDryRunPass = true,
            RequireApprovalForProduction = true
        };

        await db.DeploymentRoutes.AddRangeAsync(routeDevStage, routeStageProd);
        await db.SaveChangesAsync();
    }
}
