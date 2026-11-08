using KinTraffic.Api.Models;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Persistence;

public class KinTrafficPersistenceTests
{
    [Fact]
    public async Task OperationalGraph_PersistsAcrossDbContextInstances()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var timestamp = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

        await using (var writeContext = database.CreateContext())
        {
            var junction = new Junction { Id = "junction-01", Name = "Central Junction", CreatedAt = timestamp };
            var device = new Device
            {
                Id = "device-01",
                JunctionId = junction.Id,
                Junction = junction,
                Name = "Controller A",
                Type = "controller",
                Status = "online"
            };
            var fault = new Fault
            {
                Id = "fault-01",
                DeviceId = device.Id,
                Device = device,
                Severity = "high",
                Status = "open",
                Description = "Controller overheating",
                CreatedAt = timestamp
            };
            var workOrder = new WorkOrder
            {
                Id = "work-order-01",
                FaultId = fault.Id,
                Fault = fault,
                TechnicianId = "technician-01",
                Status = "assigned",
                Priority = "high",
                ScheduledFor = timestamp,
                CreatedAt = timestamp
            };

            writeContext.AddRange(junction, device, fault, workOrder, new Telemetry
            {
                DeviceId = device.Id,
                Device = device,
                Metric = "queue-length",
                Value = 12,
                RecordedAt = timestamp
            });
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var persistedWorkOrder = await readContext.WorkOrders
            .Include(item => item.Fault)
            .ThenInclude(item => item.Device)
            .ThenInclude(item => item.Junction)
            .SingleAsync();

        Assert.Equal("junction-01", persistedWorkOrder.Fault.Device.Junction.Id);
        Assert.Equal("device-01", persistedWorkOrder.Fault.DeviceId);
        Assert.Equal("fault-01", persistedWorkOrder.FaultId);
        Assert.Equal(1, await readContext.Telemetry.CountAsync());
    }

    [Fact]
    public async Task Device_RequiresAnExistingJunction()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.Devices.Add(new Device
        {
            Id = "device-orphan",
            JunctionId = "missing-junction",
            Name = "Controller",
            Type = "controller",
            Status = "online"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}