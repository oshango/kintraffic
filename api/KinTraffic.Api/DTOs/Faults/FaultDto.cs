namespace KinTraffic.Api.DTOs.Faults;

public class FaultDto
{
    public string Id { get; set; } = string.Empty;
    public string JunctionId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
