namespace KinTraffic.Api.DTOs.Traffic;

public class RoadSegmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CorridorName { get; set; }
    public string Geometry { get; set; } = string.Empty;
    public decimal? LengthMeters { get; set; }
}