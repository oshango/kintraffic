using System.Net;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class AuthenticationOptionsTests
{
    [Fact]
    public void InstalledIdentityHasher_DefaultsToV3With100000Iterations()
    {
        var defaults = new Microsoft.AspNetCore.Identity.PasswordHasherOptions();

        Assert.Equal(Microsoft.AspNetCore.Identity.PasswordHasherCompatibilityMode.IdentityV3, defaults.CompatibilityMode);
        Assert.Equal(100000, defaults.IterationCount);
    }

    [Fact]
    public async Task ProductionStartup_RejectsMissingAuthenticationConfiguration()
    {
        await AssertStartupFails(new Dictionary<string, string?>());
    }

    [Fact]
    public async Task ProductionStartup_RejectsMissingLockoutConfiguration()
    {
        var values = CreateValidConfiguration();
        values.Remove("Authentication:MaxFailedAccessAttempts");
        values.Remove("Authentication:LockoutDurationMinutes");

        await AssertStartupFails(values);
    }

    [Theory]
    [InlineData("Authentication:MaxFailedAccessAttempts", "9")]
    [InlineData("Authentication:LockoutDurationMinutes", "6")]
    public async Task ProductionStartup_RejectsDifferentLockoutConfiguration(string key, string value)
    {
        var values = CreateValidConfiguration();
        values[key] = value;

        await AssertStartupFails(values);
    }

    [Fact]
    public void Validator_Accepts3072BitSigningKeyAndHostCookieWithRootPath()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        var options = AuthenticationTestServices.CreateOptions();
        using var rsa = System.Security.Cryptography.RSA.Create(3072);
        options.SigningPrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
        options.RefreshCookieName = "__Host-kt-refresh";
        options.RefreshCookieSameSite = "Strict";
        options.RefreshCookiePath = "/";
        var validator = new AuthenticationOptionsValidator(builder.Environment);

        Assert.True(validator.Validate(null, options).Succeeded);
        using var keyRing = new SigningKeyRing(options);
        var tokenService = new AccessTokenService(options, keyRing, TimeProvider.System);
        var issuedToken = tokenService.Issue("test-account");
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        Assert.NotNull(handler.ValidateToken(issuedToken.Value, tokenService.CreateValidationParameters(), out _));
        options.RefreshCookiePath = "/api/auth";
        Assert.True(validator.Validate(null, options).Failed);
    }

    [Theory]
    [InlineData("Authentication:PasswordMinimumLength", "14")]
    [InlineData("Authentication:PasswordMaximumLength", "127")]
    [InlineData("Authentication:PasswordMaximumLength", "129")]
    [InlineData("Authentication:PasswordHasherIterationCount", "219999")]
    public async Task ProductionStartup_RejectsWeakPasswordConfiguration(string key, string value)
    {
        var values = CreateValidConfiguration();
        values[key] = value;

        await AssertStartupFails(values);
    }

    [Fact]
    public async Task ProductionStartup_RejectsMissingPasswordConfiguration()
    {
        var values = CreateValidConfiguration();
        values.Remove("Authentication:PasswordMinimumLength");
        values.Remove("Authentication:PasswordMaximumLength");
        values.Remove("Authentication:PasswordHasherIterationCount");

        await AssertStartupFails(values);
    }

    [Fact]
    public async Task ProductionStartup_RejectsMalformedSigningKey()
    {
        var values = CreateValidConfiguration();
        values["Authentication:SigningPrivateKeyPem"] = "not-a-key";

        await AssertStartupFails(values);
    }

    [Fact]
    public async Task ProductionStartup_RejectsNonHttpsAllowedOrigin()
    {
        var values = CreateValidConfiguration();
        values["Authentication:AllowedOrigins:0"] = "http://ui.test";

        await AssertStartupFails(values);
    }

    [Fact]
    public async Task ProductionStartup_RejectsMultipleAllowedOrigins()
    {
        var values = CreateValidConfiguration();
        values["Authentication:AllowedOrigins:1"] = "https://second-ui.test";

        await AssertStartupFails(values);
    }

    [Fact]
    public async Task ProductionStartup_Rejects2048BitSigningKey()
    {
        var values = CreateValidConfiguration();
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        values["Authentication:SigningPrivateKeyPem"] = rsa.ExportPkcs8PrivateKeyPem();

        await AssertStartupFails(values);
    }

    [Fact]
    public async Task ProductionStartup_Rejects2048BitPreviousSigningKey()
    {
        var values = CreateValidConfiguration();
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        values["Authentication:PreviousSigningKeys:0:KeyId"] = "previous-key";
        values["Authentication:PreviousSigningKeys:0:PublicKeyPem"] = rsa.ExportSubjectPublicKeyInfoPem();

        await AssertStartupFails(values);
    }

    [Theory]
    [InlineData("Authentication:MaxAccountThrottlePartitions", "9999")]
    [InlineData("Authentication:MaxAccountThrottlePartitions", "10001")]
    [InlineData("Authentication:AccountThrottleIdleMinutes", "29")]
    public async Task ProductionStartup_RejectsNonstandardThrottleCapacityOrIdleRetention(string key, string value)
    {
        var values = CreateValidConfiguration();
        values[key] = value;

        await AssertStartupFails(values);
    }

    [Fact]
    public async Task ProductionStartup_RejectsThrottleIdleRetentionShorterThanAccountWindow()
    {
        var values = CreateValidConfiguration();
        values["Authentication:AccountWindowSeconds"] = "1801";

        await AssertStartupFails(values);
    }

    [Fact]
    public void Development_AllowsMissingOriginsWithoutAddingCorsConfiguration()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        var options = AuthenticationTestServices.CreateOptions();
        options.AllowedOrigins = null;
        var validator = new AuthenticationOptionsValidator(builder.Environment);

        Assert.True(validator.Validate(null, options).Succeeded);
    }

    private static async Task AssertStartupFails(Dictionary<string, string?> values)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Configuration.AddInMemoryCollection(values);
        builder.Services.AddSingleton<IValidateOptions<AuthenticationOptions>, AuthenticationOptionsValidator>();
        builder.Services.AddOptions<AuthenticationOptions>()
            .Bind(builder.Configuration.GetSection(AuthenticationOptions.SectionName))
            .ValidateOnStart();
        await using var app = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
    }

    private static Dictionary<string, string?> CreateValidConfiguration()
    {
        var options = AuthenticationTestServices.CreateOptions();
        using var rsa = System.Security.Cryptography.RSA.Create(3072);
        options.SigningPrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
        return new Dictionary<string, string?>
        {
            ["Authentication:Issuer"] = options.Issuer,
            ["Authentication:Audience"] = options.Audience,
            ["Authentication:AccessTokenLifetimeMinutes"] = options.AccessTokenLifetimeMinutes.ToString(),
            ["Authentication:ClockSkewSeconds"] = options.ClockSkewSeconds.ToString(),
            ["Authentication:RefreshIdleLifetimeMinutes"] = options.RefreshIdleLifetimeMinutes.ToString(),
            ["Authentication:RefreshAbsoluteLifetimeDays"] = options.RefreshAbsoluteLifetimeDays.ToString(),
            ["Authentication:RefreshCookieName"] = options.RefreshCookieName,
            ["Authentication:RefreshCookieSameSite"] = options.RefreshCookieSameSite,
            ["Authentication:RefreshCookiePath"] = options.RefreshCookiePath,
            ["Authentication:AllowedOrigins:0"] = options.AllowedOrigins![0],
            ["Authentication:TrustedProxies:0"] = options.TrustedProxies![0],
            ["Authentication:SigningKeyId"] = options.SigningKeyId,
            ["Authentication:SigningPrivateKeyPem"] = options.SigningPrivateKeyPem,
            ["Authentication:SourceIpPermitLimit"] = options.SourceIpPermitLimit.ToString(),
            ["Authentication:SourceIpWindowSeconds"] = options.SourceIpWindowSeconds.ToString(),
            ["Authentication:AccountPermitLimit"] = options.AccountPermitLimit.ToString(),
            ["Authentication:AccountWindowSeconds"] = options.AccountWindowSeconds.ToString(),
            ["Authentication:MaxAccountThrottlePartitions"] = options.MaxAccountThrottlePartitions.ToString(),
            ["Authentication:AccountThrottleIdleMinutes"] = options.AccountThrottleIdleMinutes.ToString(),
            ["Authentication:MaxFailedAccessAttempts"] = options.MaxFailedAccessAttempts.ToString(),
            ["Authentication:LockoutDurationMinutes"] = options.LockoutDurationMinutes.ToString(),
            ["Authentication:PasswordMinimumLength"] = options.PasswordMinimumLength.ToString(),
            ["Authentication:PasswordMaximumLength"] = options.PasswordMaximumLength.ToString(),
            ["Authentication:PasswordHasherIterationCount"] = options.PasswordHasherIterationCount.ToString()
        };
    }
}