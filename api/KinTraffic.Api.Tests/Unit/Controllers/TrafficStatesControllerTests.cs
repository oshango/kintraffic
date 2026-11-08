using KinTraffic.Api.Controllers;
using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Models;
using KinTraffic.Api.Tests.TestHelpers;
using KinTraffic.Api.Repositories.Traffic;
using KinTraffic.Api.Services.Traffic;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Controllers;

public class TrafficStatesControllerTests
{
    [Fact]
    public async Task GetTrafficStates_ReturnsPersistedTrafficStates()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.RoadSegments.Add(new RoadSegment { Name = "Segment 1", Geometry = "{}" });
        await context.SaveChangesAsync();
        context.TrafficStates.Add(new TrafficState
        {
            RoadSegmentId = 1,
            CongestionLevel = "normal",
            AverageSpeedKph = 43.5m,
            VehicleCount = 18,
            RecordedAt = new DateTime(2026, 8, 30, 8, 5, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();
        var controller = new TrafficStatesController(
            new TrafficStateService(new TrafficStateRepository(context)));

        var result = await controller.GetTrafficStates(CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var states = Assert.IsAssignableFrom<IEnumerable<TrafficStateDto>>(response.Value);
        var state = Assert.Single(states);

        Assert.Equal(1, state.RoadSegmentId);
        Assert.Equal("normal", state.CongestionLevel);
    }
}