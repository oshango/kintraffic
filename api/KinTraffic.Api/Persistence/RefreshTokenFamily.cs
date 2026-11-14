namespace KinTraffic.Api.Persistence;

public sealed class RefreshTokenFamily
{
    public Guid Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public byte[] SecurityStampHash { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AbsoluteExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public ICollection<RefreshTokenRecord> Tokens { get; set; } = new List<RefreshTokenRecord>();
}