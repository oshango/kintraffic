namespace KinTraffic.Api.DTOs.Assets;

public class AssetDto
{
    public string Id { get; set; } = string.Empty;
    public string JunctionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "controller";
    public string Status { get; set; } = "online";
}
