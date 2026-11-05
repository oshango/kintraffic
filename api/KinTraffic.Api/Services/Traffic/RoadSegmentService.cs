using KinTraffic.Api.DTOs.Traffic;
using KinTraffic.Api.Models;
using KinTraffic.Api.Repositories.Traffic;

namespace KinTraffic.Api.Services.Traffic;

public class RoadSegmentService
{
    private readonly RoadSegmentRepository _repository;

    public RoadSegmentService(RoadSegmentRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<RoadSegmentDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<RoadSegmentDto?> GetByIdAsync(int id)
    {
        var segment = await _repository.GetByIdAsync(id);
        return segment is null ? null : ToDto(segment);
    }

    public async Task<RoadSegmentDto> AddAsync(RoadSegmentDto dto)
    {
        var segment = await _repository.AddAsync(new RoadSegment
        {
            Name = dto.Name,
            CorridorName = dto.CorridorName,
            Geometry = dto.Geometry,
            LengthMeters = dto.LengthMeters
        });

        return ToDto(segment);
    }

    private static RoadSegmentDto ToDto(RoadSegment segment) => new()
    {
        Id = segment.Id,
        Name = segment.Name,
        CorridorName = segment.CorridorName,
        Geometry = segment.Geometry,
        LengthMeters = segment.LengthMeters
    };
}