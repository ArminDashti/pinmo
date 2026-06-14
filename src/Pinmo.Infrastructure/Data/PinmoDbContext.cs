using Microsoft.EntityFrameworkCore;
using Pinmo.Core.Entities;

namespace Pinmo.Infrastructure.Data;

public class PinmoDbContext(DbContextOptions<PinmoDbContext> options) : DbContext(options)
{
    public DbSet<MonitoredEndpoint> MonitoredEndpoints => Set<MonitoredEndpoint>();
    public DbSet<PingRecord> PingRecords => Set<PingRecord>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MonitoredEndpoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.HttpMethod).HasMaxLength(10).IsRequired();
            entity.Property(e => e.LastErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.IsEnabled);
        });

        modelBuilder.Entity<PingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.CheckedAt);
            entity.HasIndex(e => e.MonitoredEndpointId);
            entity.HasOne(e => e.MonitoredEndpoint)
                .WithMany(e => e.PingRecords)
                .HasForeignKey(e => e.MonitoredEndpointId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(new AppSettings());
        });
    }
}
