using System.Net;
using KinTraffic.Api.Persistence;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task LoginAsync_ExistingAndUnknownAccountsHaveSameThrottleAndFailureResults()
    {
        var options = AuthenticationTestServices.CreateOptions();
        options.AccountPermitLimit = 2;
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var account = new KinTrafficIdentityAccount { UserName = "operator@example.invalid" };
        var created = await userManager.CreateAsync(account, "Test-Password-123!");
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
        var authentication = services.GetRequiredService<AuthenticationService>();
        var ip = IPAddress.Parse("192.0.2.20");

        var existingFirst = await authentication.LoginAsync(account.UserName!, "wrong-password", ip, CancellationToken.None);
        var existingSecond = await authentication.LoginAsync(account.UserName!, "wrong-password", ip, CancellationToken.None);
        var existingThird = await authentication.LoginAsync(account.UserName!, "wrong-password", ip, CancellationToken.None);

        var unknownFirst = await authentication.LoginAsync("unknown@example.invalid", "wrong-password", ip, CancellationToken.None);
        var unknownSecond = await authentication.LoginAsync("unknown@example.invalid", "wrong-password", ip, CancellationToken.None);
        var unknownThird = await authentication.LoginAsync("unknown@example.invalid", "wrong-password", ip, CancellationToken.None);

        Assert.Equal(LoginAttemptResult.Failed, existingFirst.Result);
        Assert.Equal(existingFirst.Result, existingSecond.Result);
        Assert.Equal(LoginAttemptResult.RateLimited, existingThird.Result);
        Assert.Equal(existingFirst.Result, unknownFirst.Result);
        Assert.Equal(existingSecond.Result, unknownSecond.Result);
        Assert.Equal(existingThird.Result, unknownThird.Result);
    }

    [Fact]
    public async Task LoginAsync_CapturedLogsContainNoCredentialsTokensOrStamps()
    {
        var options = AuthenticationTestServices.CreateOptions();
        var loggerProvider = new CapturingLoggerProvider();
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(
            context,
            options,
            loggerProvider: loggerProvider);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var password = $"Test-{Guid.NewGuid():N}aA1!";
        var account = new KinTrafficIdentityAccount { UserName = "operator@example.invalid" };
        var created = await userManager.CreateAsync(account, password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
        var securityStamp = await userManager.GetSecurityStampAsync(account);

        var result = await services.GetRequiredService<AuthenticationService>().LoginAsync(
            account.UserName!,
            password,
            IPAddress.Parse("192.0.2.40"),
            CancellationToken.None);
        var logs = string.Join('\n', loggerProvider.Messages);
        var refreshHash = System.Security.Cryptography.SHA256.HashData(
            Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(result.Tokens!.RefreshToken));

        Assert.Equal(LoginAttemptResult.Succeeded, result.Result);
        Assert.DoesNotContain(password, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Tokens.AccessToken, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Tokens.RefreshToken, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(securityStamp, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToHexString(refreshHash), logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(securityStamp))), logs, StringComparison.OrdinalIgnoreCase);
    }
}