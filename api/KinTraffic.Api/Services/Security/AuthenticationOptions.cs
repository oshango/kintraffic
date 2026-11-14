namespace KinTraffic.Api.Services.Security;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int? AccessTokenLifetimeMinutes { get; set; }
    public int? ClockSkewSeconds { get; set; }
    public int? RefreshIdleLifetimeMinutes { get; set; }
    public int? RefreshAbsoluteLifetimeDays { get; set; }
    public string? RefreshCookieName { get; set; }
    public string? RefreshCookieSameSite { get; set; }
    public string? RefreshCookiePath { get; set; }
    public string[]? AllowedOrigins { get; set; }
    public string[]? TrustedProxies { get; set; }
    public string? SigningKeyId { get; set; }
    public string? SigningPrivateKeyPem { get; set; }
    public SigningPublicKeyOptions[]? PreviousSigningKeys { get; set; }
    public int? SourceIpPermitLimit { get; set; }
    public int? SourceIpWindowSeconds { get; set; }
    public int? AccountPermitLimit { get; set; }
    public int? AccountWindowSeconds { get; set; }
    public int? MaxAccountThrottlePartitions { get; set; }
    public int? AccountThrottleIdleMinutes { get; set; }
    public int? MaxFailedAccessAttempts { get; set; }
    public int? LockoutDurationMinutes { get; set; }
}

public sealed class SigningPublicKeyOptions
{
    public string? KeyId { get; set; }
    public string? PublicKeyPem { get; set; }
}