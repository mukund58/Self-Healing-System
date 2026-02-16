using Microsoft.EntityFrameworkCore;
using TaskApi.Domain;
using TaskApi.Models;

namespace TaskApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<MetricRecord> MetricRecords => Set<MetricRecord>();
    public DbSet<FailureEvent> FailureEvents => Set<FailureEvent>();
    public DbSet<RecoveryAction> RecoveryActions => Set<RecoveryAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MetricRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RecordedAt);
            entity.HasIndex(e => e.MetricId);
        });

        modelBuilder.Entity<FailureEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DetectedAt);
            entity.HasIndex(e => e.FailureType);
        });

        modelBuilder.Entity<RecoveryAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FailureEventId);
            entity.HasIndex(e => e.PerformedAt);
        });

        base.OnModelCreating(modelBuilder);
    }
}
