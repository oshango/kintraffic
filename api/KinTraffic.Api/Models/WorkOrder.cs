namespace KinTraffic.Api.Models;

public class WorkOrder
{
    public string Id { get; set; } = string.Empty;
    public string FaultId { get; set; } = string.Empty;
    public string? TechnicianId { get; set; }
    public string Status { get; set; } = "queued";
    public string Priority { get; set; } = "normal";
    public DateTime ScheduledFor { get; set; }
}
