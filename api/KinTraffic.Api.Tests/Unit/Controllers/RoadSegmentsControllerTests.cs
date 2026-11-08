using KinTraffic.Api.Controllers;
using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Models;
using KinTraffic.Api.Tests.TestHelpers;
using KinTraffic.Api.Repositories.Traffic;
using KinTraffic.Api.Services.Traffic;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Controllers;

public class RoadSegmentsControllerTests
{
    [Fact]
    public async Task GetRoadSegments_ReturnsPersistedRoadSegments()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.RoadSegments.AddRange(CreateSegments());
        await context.SaveChangesAsync();
        var controller = new RoadSegmentsController(
            new RoadSegmentService(new RoadSegmentRepository(context)));

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
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = new RoadSegmentsController(
            new RoadSegmentService(new RoadSegmentRepository(context)));

        var result = await controller.GetRoadSegment(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AddRoadSegment_ReturnsCreatedSegment()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.RoadSegments.AddRange(CreateSegments());
        await context.SaveChangesAsync();
        var controller = new RoadSegmentsController(
            new RoadSegmentService(new RoadSegmentRepository(context)));
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

        await using var verificationContext = database.CreateContext();
        var persisted = await verificationContext.RoadSegments.FindAsync(segment.Id);
        Assert.NotNull(persisted);
        Assert.Equal(dto.Name, persisted.Name);
    }

    private static RoadSegment[] CreateSegments() =>
    [
        new RoadSegment { Name = "Boulevard du 30 Juin - Segment 1", CorridorName = "Boulevard du 30 Juin", Geometry = "{}", LengthMeters = 620 },
        new RoadSegment { Name = "Boulevard Lumumba - Segment 1", CorridorName = "Boulevard Lumumba", Geometry = "{}", LengthMeters = 780 },
        new RoadSegment { Name = "Avenue Kasa-Vubu - Segment 1", CorridorName = "Avenue Kasa-Vubu", Geometry = "{}", LengthMeters = 540 },
        new RoadSegment { Name = "Avenue de la Libération - Segment 1", CorridorName = "Avenue de la Libération", Geometry = "{}", LengthMeters = 690 },
        new RoadSegment { Name = "Avenue des Huileries - Segment 1", CorridorName = "Avenue des Huileries", Geometry = "{}", LengthMeters = 480 }
    ];
}