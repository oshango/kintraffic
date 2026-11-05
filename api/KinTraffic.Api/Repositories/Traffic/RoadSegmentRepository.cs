using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Traffic;

public class RoadSegmentRepository
{
    private readonly List<RoadSegment> _segments =
    [
        new RoadSegment
        {
            Id = 1,
            Name = "Boulevard du 30 Juin - Segment 1",
            CorridorName = "Boulevard du 30 Juin",
            Geometry = "{\"type\":\"LineString\",\"coordinates\":[[15.298, -4.305],[15.303, -4.308]]}",
            LengthMeters = 620
        },
        new RoadSegment
        {
            Id = 2,
            Name = "Boulevard Lumumba - Segment 1",
            CorridorName = "Boulevard Lumumba",
            Geometry = "{\"type\":\"LineString\",\"coordinates\":[[15.335, -4.321],[15.341, -4.324]]}",
            LengthMeters = 780
        },
        new RoadSegment
        {
            Id = 3,
            Name = "Avenue Kasa-Vubu - Segment 1",
            CorridorName = "Avenue Kasa-Vubu",
            Geometry = "{\"type\":\"LineString\",\"coordinates\":[[15.285, -4.322],[15.290, -4.326]]}",
            LengthMeters = 540
        },
        new RoadSegment
        {
            Id = 4,
            Name = "Avenue de la Libération - Segment 1",
            CorridorName = "Avenue de la Libération",
            Geometry = "{\"type\":\"LineString\",\"coordinates\":[[15.276, -4.315],[15.281, -4.318]]}",
            LengthMeters = 690
        },
        new RoadSegment
        {
            Id = 5,
            Name = "Avenue des Huileries - Segment 1",
            CorridorName = "Avenue des Huileries",
            Geometry = "{\"type\":\"LineString\",\"coordinates\":[[15.292, -4.310],[15.297, -4.313]]}",
            LengthMeters = 480
        }
    ];

    public Task<IReadOnlyList<RoadSegment>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<RoadSegment>>(_segments);

    public Task<RoadSegment?> GetByIdAsync(int id) =>
        Task.FromResult(_segments.FirstOrDefault(segment => segment.Id == id));

    public Task<RoadSegment> AddAsync(RoadSegment segment)
    {
        segment.Id = _segments.Count == 0 ? 1 : _segments.Max(existing => existing.Id) + 1;
        _segments.Add(segment);
        return Task.FromResult(segment);
    }
}