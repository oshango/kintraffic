namespace KinTraffic.Api.DTOs.Billing;

public class InvoiceDto
{
    public string Id { get; set; } = string.Empty;
    public string ContractId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime IssuedAt { get; set; }
    public string Status { get; set; } = "issued";
}
