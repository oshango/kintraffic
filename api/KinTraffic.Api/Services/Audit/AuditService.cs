using KinTraffic.Api.DTOs.Audit;
using KinTraffic.Api.Repositories.Audit;

namespace KinTraffic.Api.Services.Audit;

public class AuditService
{
    private readonly AuditRepository _repository;

    public AuditService(AuditRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<AuditLogDto> GetLogs() =>
        _repository.GetLogs().Select(log => new AuditLogDto
        {
            Id = log.Id,
            Entity = log.Entity,
            Action = log.Action,
            Actor = log.Actor,
            OccurredAt = log.OccurredAt
        });
}
