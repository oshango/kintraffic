namespace KinTraffic.Api.Models;

public class Fault
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string? FaultType { get; set; }
    public string Severity { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Device Device { get; set; } = null!;
}
