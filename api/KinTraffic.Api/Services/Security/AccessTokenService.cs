using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace KinTraffic.Api.Services.Security;

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);

public sealed class AccessTokenService
{
    private readonly AuthenticationOptions _options;
    private readonly SigningKeyRing _keyRing;
    private readonly TimeProvider _timeProvider;

    public AccessTokenService(
        AuthenticationOptions options,
        SigningKeyRing keyRing,
        TimeProvider timeProvider)
    {
        _options = options;
        _keyRing = keyRing;
        _timeProvider = timeProvider;
    }

    public IssuedAccessToken Issue(string accountId)
    {
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(_timeProvider.GetUtcNow().ToUnixTimeSeconds());
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenLifetimeMinutes!.Value);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = accountId,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N")
            },
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _keyRing.CurrentSigningCredentials
        };

        var handler = new JwtSecurityTokenHandler();
        return new IssuedAccessToken(handler.WriteToken(handler.CreateToken(descriptor)), expiresAt);
    }

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKeyResolver = (_, _, keyId, _) =>
            keyId is not null && _keyRing.ValidationKeys.TryGetValue(keyId, out var key)
                ? [key]
                : [],
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        LifetimeValidator = (notBefore, expires, _, _) =>
        {
            if (expires is null)
            {
                return false;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var skew = TimeSpan.FromSeconds(_options.ClockSkewSeconds!.Value);
            return (notBefore is null || notBefore <= now + skew) && expires > now - skew;
        },
        ClockSkew = TimeSpan.FromSeconds(_options.ClockSkewSeconds!.Value),
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = "role"
    };
}