using KinTraffic.Api.Models;
using KinTraffic.Api.Persistence;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Persistence;

public class KinTrafficPersistenceTests
{
    [Fact]
    public async Task IdentityAccount_PersistsHashedPasswordAcrossContexts()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var password = $"Test-{Guid.NewGuid():N}aA1!";

        await using (var writeContext = database.CreateContext())
        {
            using var services = CreateIdentityServices(writeContext);
            var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
            var account = new KinTrafficIdentityAccount { UserName = "operator@example.invalid" };

            var result = await userManager.CreateAsync(account, password);

            Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
            Assert.NotNull(account.PasswordHash);
            Assert.DoesNotContain(password, account.PasswordHash);
        }

        await using var readContext = database.CreateContext();
        using var readServices = CreateIdentityServices(readContext);
        var readManager = readServices.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var persisted = await readManager.FindByNameAsync("operator@example.invalid");

        Assert.NotNull(persisted);
        Assert.True(await readManager.CheckPasswordAsync(persisted, password));
        Assert.False(await readManager.CheckPasswordAsync(persisted, "incorrect-password"));
        Assert.Null(readContext.Model.FindEntityType(typeof(UserProfile)));
    }

    [Fact]
    public async Task IdentityAccounts_AreNotSeeded()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Assert.Empty(await context.Users.ToListAsync());
    }

    [Fact]
    public async Task IdentityCompositeKeyProperties_UseSqlServerSafeLengths()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var login = context.Model.FindEntityType(typeof(IdentityUserLogin<string>))!;
        var token = context.Model.FindEntityType(typeof(IdentityUserToken<string>))!;

        Assert.Equal(128, login.FindProperty(nameof(IdentityUserLogin<string>.LoginProvider))!.GetMaxLength());
        Assert.Equal(128, login.FindProperty(nameof(IdentityUserLogin<string>.ProviderKey))!.GetMaxLength());
        Assert.Equal(128, token.FindProperty(nameof(IdentityUserToken<string>.LoginProvider))!.GetMaxLength());
        Assert.Equal(128, token.FindProperty(nameof(IdentityUserToken<string>.Name))!.GetMaxLength());
    }

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

    private static ServiceProvider CreateIdentityServices(KinTrafficDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(context);
        services.AddIdentityCore<KinTrafficIdentityAccount>()
            .AddEntityFrameworkStores<KinTrafficDbContext>();
        return services.BuildServiceProvider();
    }
}