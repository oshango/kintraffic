namespace KinTraffic.Api.DTOs.Contracts;

public class ContractDto
{
    public string Id { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "active";
}
