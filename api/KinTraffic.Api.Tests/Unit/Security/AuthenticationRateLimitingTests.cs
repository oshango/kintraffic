using System.Net;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class AuthenticationRateLimitingTests
{
    [Fact]
    public async Task NamedPolicy_EnforcesLimitAndReturnsConsistentRetryResponse()
    {
        await using var app = await CreateAppAsync(permitLimit: 1);
        using var client = CreateClient(app);

        var firstResponse = await client.GetAsync("/login");
        using var rejectedResponse = await client.GetAsync("/login");
        var responseBody = await rejectedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(responseBody);
        Assert.Equal("rate_limit_exceeded", json.RootElement.GetProperty("error").GetString());
        Assert.True(rejectedResponse.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task NamedPolicy_PartitionsRequestsByClientIp()
    {
        await using var app = await CreateAppAsync(permitLimit: 1, overrideRemoteIp: true);
        using var client = CreateClient(app);

        using var firstClientRequest = CreateRequest("192.0.2.10");
        using var otherClientRequest = CreateRequest("192.0.2.11");
        using var repeatedClientRequest = CreateRequest("192.0.2.10");
        var firstClientResponse = await client.SendAsync(firstClientRequest);
        var otherClientResponse = await client.SendAsync(otherClientRequest);
        var repeatedClientResponse = await client.SendAsync(repeatedClientRequest);

        Assert.Equal(HttpStatusCode.OK, firstClientResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherClientResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, repeatedClientResponse.StatusCode);
    }

    [Fact]
    public async Task ForwardedClientIp_IsUsedOnlyForConfiguredTrustedProxy()
    {
        await using var untrustedProxyApp = await CreateAppAsync(permitLimit: 1);
        using var untrustedProxyClient = CreateClient(untrustedProxyApp);
        using var firstSpoofedRequest = CreateForwardedRequest("203.0.113.10");
        using var secondSpoofedRequest = CreateForwardedRequest("203.0.113.11");

        Assert.Equal(HttpStatusCode.OK, (await untrustedProxyClient.SendAsync(firstSpoofedRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await untrustedProxyClient.SendAsync(secondSpoofedRequest)).StatusCode);

        await using var trustedProxyApp = await CreateAppAsync(
            permitLimit: 1,
            trustedProxy: IPAddress.Loopback.ToString());
        using var trustedProxyClient = CreateClient(trustedProxyApp);
        using var trustedFirstRequest = CreateForwardedRequest("203.0.113.10");
        using var trustedOtherClientRequest = CreateForwardedRequest("203.0.113.11");
        using var trustedRepeatedRequest = CreateForwardedRequest("203.0.113.10");

        Assert.Equal(HttpStatusCode.OK, (await trustedProxyClient.SendAsync(trustedFirstRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await trustedProxyClient.SendAsync(trustedOtherClientRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await trustedProxyClient.SendAsync(trustedRepeatedRequest)).StatusCode);
    }

    [Fact]
    public void TrustedForwardedHeaders_AreDisabledWithoutExplicitProxyConfiguration()
    {
        var services = new ServiceCollection();
        var authenticationOptions = AuthenticationTestServices.CreateOptions();

        var useForwardedHeaders = services.AddAuthenticationRateLimiting(authenticationOptions);
        using var provider = services.BuildServiceProvider();
        var forwardedOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ForwardedHeadersOptions>>().Value;

        Assert.False(useForwardedHeaders);
        Assert.Equal(ForwardedHeaders.None, forwardedOptions.ForwardedHeaders);
    }

    private static async Task<WebApplication> CreateAppAsync(
        int permitLimit,
        bool overrideRemoteIp = false,
        string? trustedProxy = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(AuthenticationRateLimitingTests).Assembly.FullName
        });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        var authenticationOptions = AuthenticationTestServices.CreateOptions();
        authenticationOptions.SourceIpPermitLimit = permitLimit;
        authenticationOptions.TrustedProxies = trustedProxy is null ? ["none"] : [trustedProxy];
        var useForwardedHeaders = builder.Services.AddAuthenticationRateLimiting(authenticationOptions);
        var app = builder.Build();

        if (useForwardedHeaders)
        {
            app.UseForwardedHeaders();
        }

        if (overrideRemoteIp)
        {
            app.Use(async (context, next) =>
            {
                if (IPAddress.TryParse(context.Request.Headers["X-Test-Remote-IP"], out var remoteIp))
                {
                    context.Connection.RemoteIpAddress = remoteIp;
                }

                await next(context);
            });
        }

        app.UseRouting();
        app.UseRateLimiter();
        app.MapGet("/login", () => Results.Ok()).RequireRateLimiting(AuthenticationRateLimiting.PolicyName);
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!;
        return new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    private static HttpRequestMessage CreateRequest(string simulatedIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/login");
        request.Headers.Add("X-Test-Remote-IP", simulatedIp);
        return request;
    }

    private static HttpRequestMessage CreateForwardedRequest(string forwardedIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/login");
        request.Headers.Add("X-Forwarded-For", forwardedIp);
        return request;
    }
}