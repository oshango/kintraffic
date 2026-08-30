namespace KinTraffic.Api.DTOs.Traffic;

public class TrafficSignalDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "normal";
    public int CycleTime { get; set; }
    public int QueueLength { get; set; }
    public DateTime UpdatedAt { get; set; }
}
