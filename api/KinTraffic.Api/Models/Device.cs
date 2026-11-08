namespace KinTraffic.Api.Models;

public class Device
{
    public string Id { get; set; } = string.Empty;
    public string JunctionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "controller";
    public string Status { get; set; } = "online";
    public string? SerialNumber { get; set; }
    public DateTime? InstalledAt { get; set; }
    public Junction Junction { get; set; } = null!;
}