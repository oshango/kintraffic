using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class AccessTokenServiceTests
{
    [Fact]
    public void Issue_ProducesMinimalRs256TokenWithKeyId_AndValidates()
    {
        var options = AuthenticationTestServices.CreateOptions();
        using var keyRing = new SigningKeyRing(options);
        var service = new AccessTokenService(options, keyRing, TimeProvider.System);

        var issued = service.Issue("account-123");
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var token = handler.ReadJwtToken(issued.Value);
        var principal = handler.ValidateToken(issued.Value, service.CreateValidationParameters(), out _);

        Assert.Equal(SecurityAlgorithms.RsaSha256, token.Header.Alg);
        Assert.Equal(options.SigningKeyId, token.Header.Kid);
        Assert.Equal(options.Issuer, token.Issuer);
        Assert.Contains(options.Audience!, token.Audiences);
        Assert.Equal("account-123", principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(
            new[]
            {
                JwtRegisteredClaimNames.Aud,
                JwtRegisteredClaimNames.Exp,
                JwtRegisteredClaimNames.Iat,
                JwtRegisteredClaimNames.Iss,
                JwtRegisteredClaimNames.Jti,
                JwtRegisteredClaimNames.Nbf,
                JwtRegisteredClaimNames.Sub
            },
            token.Payload.Keys.OrderBy(type => type, StringComparer.Ordinal));
        Assert.Equal(issued.ExpiresAt, DateTimeOffset.FromUnixTimeSeconds(token.Payload.Expiration!.Value));
    }

    [Fact]
    public void Validate_RejectsExpiredTokenUsingInjectedTimeProvider()
    {
        var options = AuthenticationTestServices.CreateOptions();
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-11-14T12:00:00Z"));
        using var keyRing = new SigningKeyRing(options);
        var service = new AccessTokenService(options, keyRing, timeProvider);
        var token = service.Issue("account-123").Value;
        timeProvider.Advance(TimeSpan.FromMinutes(6));

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(service, token));
    }

    [Theory]
    [InlineData("malformed")]
    public void Validate_RejectsMalformedToken(string token)
    {
        var options = AuthenticationTestServices.CreateOptions();
        using var keyRing = new SigningKeyRing(options);
        var service = new AccessTokenService(options, keyRing, TimeProvider.System);

        Assert.Throws<SecurityTokenMalformedException>(() => Validate(service, token));
    }

    [Fact]
    public void Validate_RejectsTamperedSignature()
    {
        var (options, keyRing, service) = CreateService();
        using (keyRing)
        {
        var token = service.Issue("account-123").Value;
        var pieces = token.Split('.');
        pieces[2] = pieces[2][..^1] + (pieces[2][^1] == 'A' ? 'B' : 'A');

            Assert.ThrowsAny<SecurityTokenException>(() => Validate(service, string.Join('.', pieces)));
        }
    }

    [Theory]
    [InlineData("https://wrong-issuer.test", "kintraffic-api-test")]
    [InlineData("https://issuer.test", "wrong-audience")]
    public void Validate_RejectsWrongIssuerOrAudience(string issuer, string audience)
    {
        var (options, keyRing, service) = CreateService();
        using (keyRing)
        {
        var token = CreateSignedToken(options, service, issuer, audience, SecurityAlgorithms.RsaSha256);

            Assert.ThrowsAny<SecurityTokenException>(() => Validate(service, token));
        }
    }

    [Fact]
    public void Validate_RejectsWrongSigningAlgorithm()
    {
        var (options, keyRing, service) = CreateService();
        using (keyRing)
        {
        var token = CreateSignedToken(options, service, options.Issuer!, options.Audience!, SecurityAlgorithms.HmacSha256);

            Assert.Equal(new[] { SecurityAlgorithms.RsaSha256 }, service.CreateValidationParameters().ValidAlgorithms);
            Assert.ThrowsAny<SecurityTokenException>(() => Validate(service, token));
        }
    }

    [Fact]
    public void Validate_AcceptsPreviousPublicKeyByKeyId()
    {
        using var oldRsa = RSA.Create(2048);
        var options = AuthenticationTestServices.CreateOptions();
        options.PreviousSigningKeys =
        [
            new SigningPublicKeyOptions
            {
                KeyId = "previous-key",
                PublicKeyPem = oldRsa.ExportSubjectPublicKeyInfoPem()
            }
        ];
        using var oldKeyRing = new SigningKeyRing(options);
        var service = new AccessTokenService(options, oldKeyRing, TimeProvider.System);
        var token = CreateTokenWithCredentials(
            options.Issuer!,
            options.Audience!,
            new RsaSecurityKey(oldRsa) { KeyId = "previous-key" });

        Assert.Equal("account-123", Validate(service, token).FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
    }

    private static (AuthenticationOptions Options, SigningKeyRing KeyRing, AccessTokenService Service) CreateService()
    {
        var options = AuthenticationTestServices.CreateOptions();
        var keyRing = new SigningKeyRing(options);
        return (options, keyRing, new AccessTokenService(options, keyRing, TimeProvider.System));
    }

    private static string CreateSignedToken(
        AuthenticationOptions options,
        AccessTokenService service,
        string issuer,
        string audience,
        string algorithm)
    {
        if (algorithm == SecurityAlgorithms.RsaSha256)
        {
            using var keyRing = new SigningKeyRing(options);
            return CreateTokenWithCredentials(issuer, audience, keyRing.CurrentSigningCredentials.Key);
        }

        var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = options.SigningKeyId };
        return CreateTokenWithCredentials(issuer, audience, key, algorithm);
    }

    private static string CreateTokenWithCredentials(
        string issuer,
        string audience,
        SecurityKey key,
        string algorithm = SecurityAlgorithms.RsaSha256)
    {
        var credentials = new SigningCredentials(key, algorithm);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [new Claim(JwtRegisteredClaimNames.Sub, "account-123"), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))],
            now,
            now.AddMinutes(2),
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static ClaimsPrincipal Validate(AccessTokenService service, string token) =>
        new JwtSecurityTokenHandler { MapInboundClaims = false }
            .ValidateToken(token, service.CreateValidationParameters(), out _);
}