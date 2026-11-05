using KinTraffic.Api.Controllers;
using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Repositories.Traffic;
using KinTraffic.Api.Services.Traffic;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Controllers;

public class RoadSegmentsControllerTests
{
    [Fact]
    public async Task GetRoadSegments_ReturnsSeededRoadSegments()
    {
        var controller = new RoadSegmentsController(
            new RoadSegmentService(new RoadSegmentRepository()));

        var result = await controller.GetRoadSegments();

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var segments = Assert.IsAssignableFrom<IEnumerable<RoadSegmentDto>>(response.Value);
        var segment = segments.First();

        Assert.Equal(1, segment.Id);
        Assert.Equal("Boulevard du 30 Juin - Segment 1", segment.Name);
        Assert.Equal("Boulevard du 30 Juin", segment.CorridorName);
        Assert.NotEmpty(segment.Geometry);
        Assert.Equal(5, segments.Count());
    }

    [Fact]
    public async Task GetRoadSegment_ReturnsNotFoundForUnknownId()
    {
        var controller = new RoadSegmentsController(
            new RoadSegmentService(new RoadSegmentRepository()));

        var result = await controller.GetRoadSegment(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddRoadSegment_ReturnsCreatedSegment()
    {
        var controller = new RoadSegmentsController(
            new RoadSegmentService(new RoadSegmentRepository()));
        var dto = new RoadSegmentDto
        {
            Name = "Avenue Bokassa - Segment 1",
            CorridorName = "Avenue Bokassa",
            Geometry = "{\"type\":\"LineString\",\"coordinates\":[[15.30, -4.31],[15.31, -4.32]]}",
            LengthMeters = 410
        };

        var result = await controller.AddRoadSegment(dto);

        var response = Assert.IsType<CreatedAtActionResult>(result.Result);
        var segment = Assert.IsType<RoadSegmentDto>(response.Value);
        Assert.Equal(6, segment.Id);
        Assert.Equal(dto.Name, segment.Name);
    }
}