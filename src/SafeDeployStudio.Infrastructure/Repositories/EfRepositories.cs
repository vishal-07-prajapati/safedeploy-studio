using Microsoft.EntityFrameworkCore;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Repositories;
using SafeDeployStudio.Infrastructure.Data;

namespace SafeDeployStudio.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly SafeDeployDbContext _db;

    public OrganizationRepository(SafeDeployDbContext db) => _db = db;

    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Organizations.Include(o => o.Projects).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await _db.Organizations.Include(o => o.Projects).FirstOrDefaultAsync(o => o.Slug == slug.ToLowerInvariant().Trim(), ct);

    public async Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Organizations.Include(o => o.Projects).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(Organization org, CancellationToken ct = default)
    {
        await _db.Organizations.AddAsync(org, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Organization org, CancellationToken ct = default)
    {
        org.Touch();
        _db.Organizations.Update(org);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Organizations.FindAsync([id], ct);
        if (entity != null)
        {
            _db.Organizations.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}

public class ProjectRepository : IProjectRepository
{
    private readonly SafeDeployDbContext _db;

    public ProjectRepository(SafeDeployDbContext db) => _db = db;

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.DatabaseConnection)
            .Include(p => p.DeploymentRoutes)
            .Include(p => p.DatabaseConnections)
            .Include(p => p.GuardrailPolicy)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project?> GetBySlugAsync(Guid organizationId, string slug, CancellationToken ct = default) =>
        await _db.Projects
            .Include(p => p.Environments)
            .Include(p => p.DatabaseConnections)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Slug == slug.ToLowerInvariant().Trim(), ct);

    public async Task<IReadOnlyList<Project>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct = default) =>
        await _db.Projects
            .Include(p => p.Environments)
            .Include(p => p.DeploymentRecords)
            .Where(p => p.OrganizationId == organizationId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(Project project, CancellationToken ct = default)
    {
        await _db.Projects.AddAsync(project, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Project project, CancellationToken ct = default)
    {
        project.Touch();
        _db.Projects.Update(project);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Projects.FindAsync([id], ct);
        if (entity != null)
        {
            _db.Projects.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}

public class DeploymentRecordRepository : IDeploymentRecordRepository
{
    private readonly SafeDeployDbContext _db;

    public DeploymentRecordRepository(SafeDeployDbContext db) => _db = db;

    public async Task<DeploymentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.DeploymentRecords
            .Include(r => r.Objects)
            .Include(r => r.SourceEnvironment)
            .Include(r => r.TargetEnvironment)
            .Include(r => r.BackupSnapshot)
            .Include(r => r.RollbackRecord)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<DeploymentRecord>> GetRecentByProjectIdAsync(Guid projectId, int count = 50, CancellationToken ct = default) =>
        await _db.DeploymentRecords
            .Include(r => r.SourceEnvironment)
            .Include(r => r.TargetEnvironment)
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(DeploymentRecord record, CancellationToken ct = default)
    {
        await _db.DeploymentRecords.AddAsync(record, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DeploymentRecord record, CancellationToken ct = default)
    {
        record.Touch();
        _db.DeploymentRecords.Update(record);
        await _db.SaveChangesAsync(ct);
    }
}

public class BackupSnapshotRepository : IBackupSnapshotRepository
{
    private readonly SafeDeployDbContext _db;

    public BackupSnapshotRepository(SafeDeployDbContext db) => _db = db;

    public async Task<BackupSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.BackupSnapshots.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<BackupSnapshot>> GetRecentByProjectIdAsync(Guid projectId, int count = 50, CancellationToken ct = default) =>
        await _db.BackupSnapshots
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(BackupSnapshot snapshot, CancellationToken ct = default)
    {
        await _db.BackupSnapshots.AddAsync(snapshot, ct);
        await _db.SaveChangesAsync(ct);
    }
}

public class GuardrailPolicyRepository : IGuardrailPolicyRepository
{
    private readonly SafeDeployDbContext _db;

    public GuardrailPolicyRepository(SafeDeployDbContext db) => _db = db;

    public async Task<GuardrailPolicy?> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default) =>
        await _db.GuardrailPolicies.FirstOrDefaultAsync(g => g.ProjectId == projectId && g.IsActive, ct);

    public async Task AddAsync(GuardrailPolicy policy, CancellationToken ct = default)
    {
        await _db.GuardrailPolicies.AddAsync(policy, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(GuardrailPolicy policy, CancellationToken ct = default)
    {
        policy.Touch();
        _db.GuardrailPolicies.Update(policy);
        await _db.SaveChangesAsync(ct);
    }
}

public class DatabaseConnectionRepository : IDatabaseConnectionRepository
{
    private readonly SafeDeployDbContext _db;

    public DatabaseConnectionRepository(SafeDeployDbContext db) => _db = db;

    public async Task<DatabaseConnection?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.DatabaseConnections.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<DatabaseConnection>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default) =>
        await _db.DatabaseConnections
            .Where(c => c.ProjectId == projectId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(DatabaseConnection connection, CancellationToken ct = default)
    {
        await _db.DatabaseConnections.AddAsync(connection, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DatabaseConnection connection, CancellationToken ct = default)
    {
        connection.Touch();
        _db.DatabaseConnections.Update(connection);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.DatabaseConnections.FindAsync([id], ct);
        if (entity != null)
        {
            _db.DatabaseConnections.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
