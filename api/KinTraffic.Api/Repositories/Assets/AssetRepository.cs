using KinTraffic.Api.Models;

namespace KinTraffic.Api.Repositories.Assets;

public class AssetRepository
{
    private readonly List<Asset> _assets =
    [
        new Asset
        {
            Id = "asset-11",
            JunctionId = "junction-01",
            Name = "Controller A",
            Type = "controller",
            Status = "online"
        }
    ];

    public IEnumerable<Asset> GetAssets() => _assets;
}
