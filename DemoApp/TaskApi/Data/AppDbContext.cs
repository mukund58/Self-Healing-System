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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MetricRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RecordedAt);
            entity.HasIndex(e => e.MetricId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
