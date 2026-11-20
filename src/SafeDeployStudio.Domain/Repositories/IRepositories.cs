using SafeDeployStudio.Domain.Entities;

namespace SafeDeployStudio.Domain.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Organization org, CancellationToken ct = default);
    Task UpdateAsync(Organization org, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetBySlugAsync(Guid organizationId, string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    Task UpdateAsync(Project project, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IDeploymentRecordRepository
{
    Task<DeploymentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DeploymentRecord>> GetRecentByProjectIdAsync(Guid projectId, int count = 50, CancellationToken ct = default);
    Task AddAsync(DeploymentRecord record, CancellationToken ct = default);
    Task UpdateAsync(DeploymentRecord record, CancellationToken ct = default);
}

public interface IBackupSnapshotRepository
{
    Task<BackupSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BackupSnapshot>> GetRecentByProjectIdAsync(Guid projectId, int count = 50, CancellationToken ct = default);
    Task AddAsync(BackupSnapshot snapshot, CancellationToken ct = default);
}

public interface IGuardrailPolicyRepository
{
    Task<GuardrailPolicy?> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task AddAsync(GuardrailPolicy policy, CancellationToken ct = default);
    Task UpdateAsync(GuardrailPolicy policy, CancellationToken ct = default);
}

public interface IDatabaseConnectionRepository
{
    Task<DatabaseConnection?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DatabaseConnection>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    Task AddAsync(DatabaseConnection connection, CancellationToken ct = default);
    Task UpdateAsync(DatabaseConnection connection, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
