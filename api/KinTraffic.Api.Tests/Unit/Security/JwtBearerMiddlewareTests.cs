using System.Net;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class JwtBearerMiddlewareTests
{
    [Fact]
    public async Task AnonymousRequestToSampleProtectedRoute_ReturnsUnauthorized()
    {
        var options = AuthenticationTestServices.CreateOptions();
        using var keyRing = new SigningKeyRing(options);
        var tokenService = new AccessTokenService(options, keyRing, TimeProvider.System);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(JwtBearerMiddlewareTests).Assembly.FullName
        });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<AccessTokenService>((bearerOptions, accessTokens) =>
            {
                bearerOptions.MapInboundClaims = false;
                bearerOptions.IncludeErrorDetails = false;
                bearerOptions.TokenValidationParameters = accessTokens.CreateValidationParameters();
            });
        builder.Services.AddSingleton(tokenService);
        builder.Services.AddAuthorization();
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/protected", () => Results.Ok()).RequireAuthorization();
        await app.StartAsync();

        using var client = new HttpClient
        {
            BaseAddress = new Uri(app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
                .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!
                .Addresses.Single())
        };
        using var response = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await app.StopAsync();
        await app.DisposeAsync();
    }
}