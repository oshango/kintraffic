using KinTraffic.Api.DTOs.Assets;
using KinTraffic.Api.Repositories.Assets;

namespace KinTraffic.Api.Services.Assets;

public class AssetService
{
    private readonly AssetRepository _repository;

    public AssetService(AssetRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<AssetDto> GetAssets() =>
        _repository.GetAssets().Select(asset => new AssetDto
        {
            Id = asset.Id,
            JunctionId = asset.JunctionId,
            Name = asset.Name,
            Type = asset.Type,
            Status = asset.Status
        });
}
