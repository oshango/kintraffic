namespace KinTraffic.Api.Models;

public class Telemetry
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime RecordedAt { get; set; }
    public Device Device { get; set; } = null!;
}