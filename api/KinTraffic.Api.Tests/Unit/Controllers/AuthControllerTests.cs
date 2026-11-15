using System.Net;
using System.Text.Json;
using KinTraffic.Api.Controllers;
using KinTraffic.Api.OpenApi;
using KinTraffic.Api.Persistence;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_ReturnsAccessTokenHeaderAndRefreshCookieButNoCredentialDataInBody()
    {
        var options = AuthenticationTestServices.CreateOptions();
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<KinTrafficIdentityAccount>>();
        const string password = "Test-Password-123!";
        var account = new KinTrafficIdentityAccount { UserName = "operator@example.invalid" };
        var created = await userManager.CreateAsync(account, password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
        var controller = CreateController(services, options, IPAddress.Parse("192.0.2.30"));

        var action = await controller.Login(
            new LoginRequest { UserName = account.UserName!, Password = password },
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(action);
        var accessToken = controller.Response.Headers.Authorization.ToString();
        var cookie = Assert.Single(controller.Response.Headers.SetCookie.ToArray());
        var body = JsonSerializer.Serialize(response.Value);
        Assert.StartsWith("Bearer ", accessToken, StringComparison.Ordinal);
        Assert.Contains("Secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HttpOnly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(accessToken, body, StringComparison.Ordinal);
        Assert.DoesNotContain(password, body, StringComparison.Ordinal);
        Assert.DoesNotContain(account.Id, body, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("roles", body, StringComparison.OrdinalIgnoreCase);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(new[] { "ExpiresAt" }, json.RootElement.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public async Task Login_UnknownAndInvalidAccountsReturnSameUnauthorizedBody()
    {
        var options = AuthenticationTestServices.CreateOptions();
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<KinTrafficIdentityAccount>>();
        var account = new KinTrafficIdentityAccount { UserName = "operator@example.invalid" };
        var created = await userManager.CreateAsync(account, "Test-Password-123!");
        Assert.True(created.Succeeded);

        var knownController = CreateController(services, options, IPAddress.Parse("192.0.2.31"));
        var unknownController = CreateController(services, options, IPAddress.Parse("192.0.2.31"));
        var knownResult = await knownController.Login(
            new LoginRequest { UserName = account.UserName!, Password = "wrong-password" },
            CancellationToken.None);
        var unknownResult = await unknownController.Login(
            new LoginRequest { UserName = "unknown@example.invalid", Password = "wrong-password" },
            CancellationToken.None);

        var knownResponse = Assert.IsAssignableFrom<ObjectResult>(knownResult);
        var unknownResponse = Assert.IsAssignableFrom<ObjectResult>(unknownResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, knownResponse.StatusCode);
        Assert.Equal(knownResponse.StatusCode, unknownResponse.StatusCode);
        Assert.Equal(JsonSerializer.Serialize(knownResponse.Value), JsonSerializer.Serialize(unknownResponse.Value));
        Assert.Equal(0, knownController.Response.Headers.SetCookie.Count);
        Assert.Equal(0, unknownController.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Login_LockedOutAndBadPasswordReturnSameUnauthorizedBody()
    {
        var options = AuthenticationTestServices.CreateOptions();
        options.MaxFailedAccessAttempts = 1;
        options.AccountPermitLimit = 10;
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<KinTrafficIdentityAccount>>();
        var account = new KinTrafficIdentityAccount { UserName = "operator@example.invalid" };
        var created = await userManager.CreateAsync(account, "Test-Password-123!");
        Assert.True(created.Succeeded);

        var badPasswordController = CreateController(services, options, IPAddress.Parse("192.0.2.34"));
        var lockedOutController = CreateController(services, options, IPAddress.Parse("192.0.2.34"));
        var badPasswordResponse = Assert.IsAssignableFrom<ObjectResult>(await badPasswordController.Login(
            new LoginRequest { UserName = account.UserName!, Password = "wrong-password" },
            CancellationToken.None));
        var lockedOutResponse = Assert.IsAssignableFrom<ObjectResult>(await lockedOutController.Login(
            new LoginRequest { UserName = account.UserName!, Password = "Test-Password-123!" },
            CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, badPasswordResponse.StatusCode);
        Assert.Equal(badPasswordResponse.StatusCode, lockedOutResponse.StatusCode);
        Assert.Equal(JsonSerializer.Serialize(badPasswordResponse.Value), JsonSerializer.Serialize(lockedOutResponse.Value));
    }

    [Fact]
    public async Task Refresh_RequiresCsrfHeaderOriginAndFetchMetadata()
    {
        var options = AuthenticationTestServices.CreateOptions();
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var controller = CreateController(services, options, IPAddress.Parse("192.0.2.32"));
        controller.Request.Headers.Origin = options.AllowedOrigins![0];
        controller.Request.Headers["Sec-Fetch-Site"] = "same-site";

        var result = await controller.Refresh(CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Theory]
    [InlineData("same-origin", true)]
    [InlineData("same-site", true)]
    [InlineData("cross-site", false)]
    [InlineData("none", false)]
    [InlineData("", false)]
    public async Task Refresh_OnlyAllowsSameOriginAndSameSiteFetchMetadata(string fetchSite, bool allowed)
    {
        var options = AuthenticationTestServices.CreateOptions();
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var controller = CreateController(services, options, IPAddress.Parse("192.0.2.33"));
        controller.Request.Headers["X-KinTraffic-CSRF"] = "1";
        controller.Request.Headers.Origin = options.AllowedOrigins![0];
        if (fetchSite.Length > 0)
        {
            controller.Request.Headers["Sec-Fetch-Site"] = fetchSite;
        }

        var result = await controller.Refresh(CancellationToken.None);

        if (allowed)
        {
            Assert.IsType<UnauthorizedObjectResult>(result);
        }
        else
        {
            Assert.IsType<ForbidResult>(result);
        }
    }

    [Theory]
    [InlineData("same-origin", true)]
    [InlineData("same-site", true)]
    [InlineData("cross-site", false)]
    [InlineData("none", false)]
    [InlineData("", false)]
    public async Task Logout_OnlyAllowsSameOriginAndSameSiteFetchMetadata(string fetchSite, bool allowed)
    {
        var options = AuthenticationTestServices.CreateOptions();
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var controller = CreateController(services, options, IPAddress.Parse("192.0.2.35"));
        controller.Request.Headers["X-KinTraffic-CSRF"] = "1";
        controller.Request.Headers.Origin = options.AllowedOrigins![0];
        if (fetchSite.Length > 0)
        {
            controller.Request.Headers["Sec-Fetch-Site"] = fetchSite;
        }

        var result = await controller.Logout(CancellationToken.None);

        if (allowed)
        {
            Assert.IsType<NoContentResult>(result);
        }
        else
        {
            Assert.IsType<ForbidResult>(result);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshAndLogout_RejectForeignOrigin(bool logout)
    {
        var options = AuthenticationTestServices.CreateOptions();
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var controller = CreateController(services, options, IPAddress.Parse("192.0.2.36"));
        controller.Request.Headers["X-KinTraffic-CSRF"] = "1";
        controller.Request.Headers.Origin = "https://attacker.test";
        controller.Request.Headers["Sec-Fetch-Site"] = "same-site";

        var result = logout
            ? await controller.Logout(CancellationToken.None)
            : await controller.Refresh(CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshAndLogout_RequireCustomCsrfHeader(bool logout)
    {
        var options = AuthenticationTestServices.CreateOptions();
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var controller = CreateController(services, options, IPAddress.Parse("192.0.2.37"));
        controller.Request.Headers.Origin = options.AllowedOrigins![0];
        controller.Request.Headers["Sec-Fetch-Site"] = "same-site";

        var result = logout
            ? await controller.Logout(CancellationToken.None)
            : await controller.Refresh(CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public async Task OpenApiDocument_IsDevelopmentOnlyAndDocumentsRefreshLogoutResponses(
        string environmentName,
        bool expectedAvailable)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName,
            ApplicationName = typeof(AuthController).Assembly.FullName
        });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddKinTrafficOpenApi();

        await using var app = builder.Build();
        app.MapOpenApiInDevelopment();
        app.MapControllers();
        await app.StartAsync();

        try
        {
            var address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            using var response = await client.GetAsync("/openapi/v1.json");

            if (!expectedAvailable)
            {
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                return;
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var paths = document.RootElement.GetProperty("paths");
            var refreshResponses = paths.GetProperty("/api/auth/refresh")
                .GetProperty("post")
                .GetProperty("responses");
            var logoutResponses = paths.GetProperty("/api/auth/logout")
                .GetProperty("post")
                .GetProperty("responses");

            Assert.Contains("200", refreshResponses.EnumerateObject().Select(response => response.Name));
            Assert.Contains("401", refreshResponses.EnumerateObject().Select(response => response.Name));
            Assert.Contains("403", refreshResponses.EnumerateObject().Select(response => response.Name));
            Assert.Contains("429", refreshResponses.EnumerateObject().Select(response => response.Name));
            Assert.Contains("204", logoutResponses.EnumerateObject().Select(response => response.Name));
            Assert.Contains("403", logoutResponses.EnumerateObject().Select(response => response.Name));
            Assert.Contains("429", logoutResponses.EnumerateObject().Select(response => response.Name));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static AuthController CreateController(
        ServiceProvider services,
        AuthenticationOptions options,
        IPAddress remoteIpAddress)
    {
        var controller = new AuthController(
            services.GetRequiredService<AuthenticationService>(),
            Options.Create(options));
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Connection.RemoteIpAddress = remoteIpAddress;
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}