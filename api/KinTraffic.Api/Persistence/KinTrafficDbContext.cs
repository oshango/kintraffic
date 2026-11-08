using KinTraffic.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Persistence;

public class KinTrafficDbContext(DbContextOptions<KinTrafficDbContext> options) : DbContext(options)
{
    public DbSet<Junction> Junctions => Set<Junction>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Fault> Faults => Set<Fault>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Telemetry> Telemetry => Set<Telemetry>();
    public DbSet<RoadSegment> RoadSegments => Set<RoadSegment>();
    public DbSet<TrafficState> TrafficStates => Set<TrafficState>();
    public DbSet<TrafficSignal> TrafficSignals => Set<TrafficSignal>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utcTimestampDefault = Database.IsSqlServer() ? "SYSUTCDATETIME()" : "CURRENT_TIMESTAMP";

        var junctions = modelBuilder.Entity<Junction>();
        junctions.ToTable("Junctions");
        junctions.HasKey(junction => junction.Id);
        junctions.Property(junction => junction.Id).HasMaxLength(64);
        junctions.Property(junction => junction.Name).HasMaxLength(100).IsRequired();
        junctions.Property(junction => junction.Location).HasMaxLength(200);
        junctions.Property(junction => junction.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql(utcTimestampDefault);

        var devices = modelBuilder.Entity<Device>();
        devices.ToTable("Devices");
        devices.HasKey(device => device.Id);
        devices.Property(device => device.Id).HasMaxLength(64);
        devices.Property(device => device.JunctionId).HasMaxLength(64).IsRequired();
        devices.Property(device => device.Name).HasMaxLength(100).IsRequired();
        devices.Property(device => device.Type).HasMaxLength(50).IsRequired();
        devices.Property(device => device.Status).HasMaxLength(50).IsRequired();
        devices.Property(device => device.SerialNumber).HasMaxLength(100);
        devices.Property(device => device.InstalledAt).HasColumnType("datetime2");
        devices.HasOne(device => device.Junction)
            .WithMany()
            .HasForeignKey(device => device.JunctionId)
            .OnDelete(DeleteBehavior.Restrict);
        devices.HasIndex(device => device.JunctionId);

        var faults = modelBuilder.Entity<Fault>();
        faults.ToTable("Faults");
        faults.HasKey(fault => fault.Id);
        faults.Property(fault => fault.Id).HasMaxLength(64);
        faults.Property(fault => fault.DeviceId).HasMaxLength(64).IsRequired();
        faults.Property(fault => fault.FaultType).HasMaxLength(100);
        faults.Property(fault => fault.Severity).HasMaxLength(32).IsRequired();
        faults.Property(fault => fault.Status).HasMaxLength(32).IsRequired();
        faults.Property(fault => fault.Description).HasMaxLength(1000).IsRequired();
        faults.Property(fault => fault.CreatedAt)
            .HasColumnName("DetectedAt")
            .HasColumnType("datetime2")
            .HasDefaultValueSql(utcTimestampDefault);
        faults.Property(fault => fault.ResolvedAt).HasColumnType("datetime2");
        faults.HasOne(fault => fault.Device)
            .WithMany()
            .HasForeignKey(fault => fault.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
        faults.HasIndex(fault => new { fault.DeviceId, fault.Status });

        var workOrders = modelBuilder.Entity<WorkOrder>();
        workOrders.ToTable("WorkOrders");
        workOrders.HasKey(workOrder => workOrder.Id);
        workOrders.Property(workOrder => workOrder.Id).HasMaxLength(64);
        workOrders.Property(workOrder => workOrder.FaultId).HasMaxLength(64).IsRequired();
        workOrders.Property(workOrder => workOrder.TechnicianId)
            .HasColumnName("AssignedTo")
            .HasMaxLength(64);
        workOrders.Property(workOrder => workOrder.Status).HasMaxLength(32).IsRequired();
        workOrders.Property(workOrder => workOrder.Priority).HasMaxLength(32).IsRequired();
        workOrders.Property(workOrder => workOrder.ScheduledFor).HasColumnType("datetime2");
        workOrders.Property(workOrder => workOrder.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql(utcTimestampDefault);
        workOrders.Property(workOrder => workOrder.CompletedAt).HasColumnType("datetime2");
        workOrders.HasOne(workOrder => workOrder.Fault)
            .WithMany()
            .HasForeignKey(workOrder => workOrder.FaultId)
            .OnDelete(DeleteBehavior.Restrict);
        workOrders.HasIndex(workOrder => workOrder.FaultId);
        workOrders.HasIndex(workOrder => workOrder.TechnicianId);

        var telemetry = modelBuilder.Entity<Telemetry>();
        telemetry.ToTable("Telemetry");
        telemetry.HasKey(record => record.Id);
        telemetry.Property(record => record.Id).ValueGeneratedOnAdd();
        telemetry.Property(record => record.DeviceId).HasMaxLength(64).IsRequired();
        telemetry.Property(record => record.Metric).HasMaxLength(50).IsRequired();
        telemetry.Property(record => record.Value).HasColumnType("float");
        telemetry.Property(record => record.RecordedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql(utcTimestampDefault);
        telemetry.HasOne(record => record.Device)
            .WithMany()
            .HasForeignKey(record => record.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
        telemetry.HasIndex(record => new { record.DeviceId, record.RecordedAt });

        var roadSegments = modelBuilder.Entity<RoadSegment>();
        roadSegments.ToTable("RoadSegments");
        roadSegments.HasKey(segment => segment.Id);
        roadSegments.Property(segment => segment.Id).ValueGeneratedOnAdd();
        roadSegments.Property(segment => segment.Name).HasMaxLength(150).IsRequired();
        roadSegments.Property(segment => segment.CorridorName).HasMaxLength(150);
        roadSegments.Property(segment => segment.Geometry)
            .HasColumnType(Database.IsSqlServer() ? "nvarchar(max)" : "TEXT")
            .IsRequired();
        roadSegments.Property(segment => segment.LengthMeters).HasColumnType("decimal(10,2)");

        var trafficStates = modelBuilder.Entity<TrafficState>();
        trafficStates.ToTable("TrafficStates");
        trafficStates.HasKey(state => state.Id);
        trafficStates.Property(state => state.Id).ValueGeneratedOnAdd();
        trafficStates.Property(state => state.CongestionLevel).HasMaxLength(50).IsRequired();
        trafficStates.Property(state => state.AverageSpeedKph).HasColumnType("decimal(10,2)");
        trafficStates.Property(state => state.RecordedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql(utcTimestampDefault);
        trafficStates.HasOne<RoadSegment>()
            .WithMany()
            .HasForeignKey(state => state.RoadSegmentId)
            .OnDelete(DeleteBehavior.Restrict);
        trafficStates.HasIndex(state => new { state.RoadSegmentId, state.RecordedAt });

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

        var inventoryItems = modelBuilder.Entity<InventoryItem>();
        inventoryItems.ToTable("InventoryItems");
        inventoryItems.HasKey(item => item.Id);
        inventoryItems.Property(item => item.Id).HasMaxLength(64);
        inventoryItems.Property(item => item.Sku).HasMaxLength(64).IsRequired();
        inventoryItems.Property(item => item.Name).HasMaxLength(200).IsRequired();
        inventoryItems.Property(item => item.Location).HasMaxLength(100).IsRequired();
        inventoryItems.HasIndex(item => item.Sku);

        var contracts = modelBuilder.Entity<Contract>();
        contracts.ToTable("Contracts");
        contracts.HasKey(contract => contract.Id);
        contracts.Property(contract => contract.Id).HasMaxLength(64);
        contracts.Property(contract => contract.VendorName).HasMaxLength(200).IsRequired();
        contracts.Property(contract => contract.ServiceType).HasMaxLength(100).IsRequired();
        contracts.Property(contract => contract.StartDate).HasColumnType("date");
        contracts.Property(contract => contract.EndDate).HasColumnType("date");
        contracts.Property(contract => contract.Status).HasMaxLength(32).IsRequired();

        var invoices = modelBuilder.Entity<Invoice>();
        invoices.ToTable("Invoices");
        invoices.HasKey(invoice => invoice.Id);
        invoices.Property(invoice => invoice.Id).HasMaxLength(64);
        invoices.Property(invoice => invoice.ContractId).HasMaxLength(64).IsRequired();
        invoices.Property(invoice => invoice.Amount).HasColumnType("decimal(18,2)");
        invoices.Property(invoice => invoice.IssuedAt).HasColumnType("datetime2");
        invoices.Property(invoice => invoice.Status).HasMaxLength(32).IsRequired();
        invoices.HasIndex(invoice => invoice.ContractId);

        var auditLogs = modelBuilder.Entity<AuditLog>();
        auditLogs.ToTable("AuditLogs");
        auditLogs.HasKey(log => log.Id);
        auditLogs.Property(log => log.Id).HasMaxLength(64);
        auditLogs.Property(log => log.Entity).HasMaxLength(128).IsRequired();
        auditLogs.Property(log => log.Action).HasMaxLength(64).IsRequired();
        auditLogs.Property(log => log.Actor).HasMaxLength(128).IsRequired();
        auditLogs.Property(log => log.OccurredAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql(utcTimestampDefault);
        auditLogs.HasIndex(log => new { log.Entity, log.OccurredAt });
    }
}