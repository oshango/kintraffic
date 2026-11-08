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

    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken cancellationToken = default) =>
        (await _repository.GetAssetsAsync(cancellationToken)).Select(device => new AssetDto
        {
            Id = device.Id,
            JunctionId = device.JunctionId,
            Name = device.Name,
            Type = device.Type,
            Status = device.Status
        }).ToList();
}
