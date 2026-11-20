using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeDeployStudio.AI.Services;
using SafeDeployStudio.Application.Services;
using SafeDeployStudio.Backup;
using SafeDeployStudio.DatabaseProviders.Abstractions;
using SafeDeployStudio.DatabaseProviders.SqlServer;
using SafeDeployStudio.Deployment;
using SafeDeployStudio.Domain.Repositories;
using SafeDeployStudio.Guardrails;
using SafeDeployStudio.Infrastructure.Data;
using SafeDeployStudio.Infrastructure.Repositories;

namespace SafeDeployStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSafeDeployStudioCore(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // 1. Storage / DbContext Setup
        var defaultDbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SafeDeployStudio", "safedeploy.db");

        var appFolder = Path.GetDirectoryName(defaultDbPath)!;
        if (!Directory.Exists(appFolder))
            Directory.CreateDirectory(appFolder);

        var sqliteConnStr = configuration?.GetConnectionString("DefaultConnection") 
            ?? $"Data Source={defaultDbPath}";

        services.AddDbContext<SafeDeployDbContext>(options =>
        {
            options.UseSqlite(sqliteConnStr);
        });

        // 2. Repositories
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IDeploymentRecordRepository, DeploymentRecordRepository>();
        services.AddScoped<IBackupSnapshotRepository, BackupSnapshotRepository>();
        services.AddScoped<IGuardrailPolicyRepository, GuardrailPolicyRepository>();
        services.AddScoped<IDatabaseConnectionRepository, DatabaseConnectionRepository>();

        // 3. Database Providers
        services.AddSingleton<IScriptBatchSplitter, SqlServerBatchSplitter>();
        services.AddScoped<IDatabaseProvider, SqlServerDatabaseProvider>();
        services.AddScoped<IDatabaseProviderFactory, DatabaseProviderFactory>();

        // 4. Guardrails Engine
        services.AddScoped<IGuardrailEngine, GuardrailEngine>();

        // 5. Diff Engine
        services.AddSingleton<ISqlDiffEngine, SqlDiffEngine>();

        // 6. Backup Vault & Rollback
        services.AddScoped<IBackupVaultService, BackupVaultService>();

        // 7. Deployment Engine
        services.AddScoped<IDeploymentService, DeploymentService>();

        // 8. AI Advisor & SARGability
        services.AddSingleton<ISargabilityAnalyzer, SargabilityAnalyzer>();
        services.AddHttpClient("SafeDeployAiAdvisor");
        services.AddScoped<IAiAdvisorService, AiAdvisorService>();

        // 9. Application Workflow
        services.AddScoped<IWorkflowService, WorkflowService>();

        return services;
    }
}
