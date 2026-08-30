namespace KinTraffic.Api.Models;

public class Asset
{
    public string Id { get; set; } = string.Empty;
    public string JunctionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "controller";
    public string Status { get; set; } = "online";
}
