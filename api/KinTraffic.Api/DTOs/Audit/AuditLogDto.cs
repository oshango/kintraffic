namespace KinTraffic.Api.DTOs.Audit;

public class AuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
