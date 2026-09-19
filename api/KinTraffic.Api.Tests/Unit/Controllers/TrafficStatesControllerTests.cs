using KinTraffic.Api.Controllers;
using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Repositories.Traffic;
using KinTraffic.Api.Services.Traffic;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Controllers;

public class TrafficStatesControllerTests
{
    [Fact]
    public void GetTrafficStates_ReturnsSeededTrafficStates()
    {
        var controller = new TrafficStatesController(
            new TrafficStateService(new TrafficStateRepository()));

        var result = controller.GetTrafficStates();

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var states = Assert.IsAssignableFrom<IEnumerable<TrafficStateDto>>(response.Value);
        var state = Assert.Single(states);

        Assert.Equal(1, state.RoadSegmentId);
        Assert.Equal("normal", state.CongestionLevel);
    }
}