using Microsoft.EntityFrameworkCore;
using SafeDeployStudio.Domain.Entities;

namespace SafeDeployStudio.Infrastructure.Data;

public class SafeDeployDbContext : DbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<EnvironmentDefinition> Environments => Set<EnvironmentDefinition>();
    public DbSet<DatabaseConnection> DatabaseConnections => Set<DatabaseConnection>();
    public DbSet<DeploymentRoute> DeploymentRoutes => Set<DeploymentRoute>();
    public DbSet<DeploymentRecord> DeploymentRecords => Set<DeploymentRecord>();
    public DbSet<DeploymentObject> DeploymentObjects => Set<DeploymentObject>();
    public DbSet<BackupSnapshot> BackupSnapshots => Set<BackupSnapshot>();
    public DbSet<RollbackRecord> RollbackRecords => Set<RollbackRecord>();
    public DbSet<GuardrailPolicy> GuardrailPolicies => Set<GuardrailPolicy>();

    public SafeDeployDbContext(DbContextOptions<SafeDeployDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>(b =>
        {
            b.HasKey(o => o.Id);
            b.HasIndex(o => o.Slug).IsUnique();
            b.Property(o => o.Name).HasMaxLength(150).IsRequired();
            b.Property(o => o.Slug).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Project>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => new { p.OrganizationId, p.Slug }).IsUnique();
            b.Property(p => p.Name).HasMaxLength(150).IsRequired();
            b.Property(p => p.Slug).HasMaxLength(100).IsRequired();
            b.HasOne(p => p.Organization)
             .WithMany(o => o.Projects)
             .HasForeignKey(p => p.OrganizationId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.GuardrailPolicy)
             .WithOne(g => g.Project)
             .HasForeignKey<GuardrailPolicy>(g => g.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnvironmentDefinition>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(100).IsRequired();
            b.HasOne(e => e.Project)
             .WithMany(p => p.Environments)
             .HasForeignKey(e => e.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DatabaseConnection>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(150).IsRequired();
            b.Property(c => c.Server).HasMaxLength(255).IsRequired();
            b.Property(c => c.DatabaseName).HasMaxLength(150).IsRequired();
            b.HasOne(c => c.Project)
             .WithMany(p => p.DatabaseConnections)
             .HasForeignKey(c => c.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeploymentRecord>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Title).HasMaxLength(255).IsRequired();
            b.HasOne(r => r.Project)
             .WithMany(p => p.DeploymentRecords)
             .HasForeignKey(r => r.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(r => r.BackupSnapshot)
             .WithOne(s => s.DeploymentRecord)
             .HasForeignKey<DeploymentRecord>(r => r.BackupSnapshotId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(r => r.RollbackRecord)
             .WithOne(rb => rb.DeploymentRecord)
             .HasForeignKey<RollbackRecord>(rb => rb.DeploymentRecordId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeploymentObject>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.ObjectName).HasMaxLength(255).IsRequired();
            b.Property(o => o.SchemaName).HasMaxLength(100).IsRequired();
            b.HasOne(o => o.DeploymentRecord)
             .WithMany(r => r.Objects)
             .HasForeignKey(o => o.DeploymentRecordId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BackupSnapshot>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.SnapshotName).HasMaxLength(255).IsRequired();
            b.Property(s => s.Sha256Checksum).HasMaxLength(64).IsRequired();
            b.HasOne(s => s.Project)
             .WithMany(p => p.BackupSnapshots)
             .HasForeignKey(s => s.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GuardrailPolicy>(b =>
        {
            b.HasKey(g => g.Id);
            b.Property(g => g.Name).HasMaxLength(150).IsRequired();
        });
    }
}
