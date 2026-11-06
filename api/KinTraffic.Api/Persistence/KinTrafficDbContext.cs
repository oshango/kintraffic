using KinTraffic.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Persistence;

public class KinTrafficDbContext(DbContextOptions<KinTrafficDbContext> options) : DbContext(options)
{
    public DbSet<TrafficSignal> TrafficSignals => Set<TrafficSignal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var signals = modelBuilder.Entity<TrafficSignal>();
        signals.ToTable("TrafficSignals");
        signals.HasKey(signal => signal.Id);
        signals.Property(signal => signal.Id).HasMaxLength(64);
        signals.Property(signal => signal.Name).HasMaxLength(200).IsRequired();
        signals.Property(signal => signal.Status).HasMaxLength(32).IsRequired();
        signals.HasData(
            new TrafficSignal
            {
                Id = "signal-001",
                Name = "Main Street & 1st Ave",
                Status = "normal",
                CycleTime = 42,
                QueueLength = 12,
                UpdatedAt = new DateTime(2026, 8, 30, 8, 0, 0, DateTimeKind.Utc)
            },
            new TrafficSignal
            {
                Id = "signal-002",
                Name = "Oak Blvd & Market Rd",
                Status = "warning",
                CycleTime = 58,
                QueueLength = 24,
                UpdatedAt = new DateTime(2026, 8, 30, 8, 3, 0, DateTimeKind.Utc)
            });
    }
}