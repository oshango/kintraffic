namespace KinTraffic.Api.Models;

public class TrafficState
{
    public int Id { get; set; }
    public int RoadSegmentId { get; set; }
    public string CongestionLevel { get; set; } = string.Empty;
    public decimal AverageSpeedKph { get; set; }
    public int VehicleCount { get; set; }
    public DateTime RecordedAt { get; set; }
}