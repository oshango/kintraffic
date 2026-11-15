namespace KinTraffic.Api.Persistence;

public sealed class RefreshTokenRecord
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public RefreshTokenFamily Family { get; set; } = null!;
}