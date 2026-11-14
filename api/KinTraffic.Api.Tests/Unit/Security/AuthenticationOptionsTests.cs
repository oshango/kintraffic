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
            ["Authentication:LockoutDurationMinutes"] = options.LockoutDurationMinutes.ToString()
        };
    }
}