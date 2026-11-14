using KinTraffic.Api.Persistence;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class RefreshTokenServiceTests
{
    [Fact]
    public async Task RotateAsync_ValidTokenConsumesOldAndReturnsNewOpaqueToken()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-11-14T12:00:00Z"));
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, timeProvider: clock);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var account = await CreateAccountAsync(userManager);
        var tokenService = services.GetRequiredService<RefreshTokenService>();

        var session = await tokenService.CreateFamilyAsync(account, CancellationToken.None);
        var rawRefreshBytes = WebEncoders.Base64UrlDecode(session.RefreshToken);
        var oldHash = System.Security.Cryptography.SHA256.HashData(rawRefreshBytes);
        var result = await tokenService.RotateAsync(session.RefreshToken, CancellationToken.None);
        var rows = await context.RefreshTokens.AsNoTracking().ToListAsync();

        Assert.Equal(32, rawRefreshBytes.Length);
        Assert.All(rows, row => Assert.Equal(32, row.TokenHash.Length));
        Assert.DoesNotContain(rows, row => row.TokenHash.SequenceEqual(rawRefreshBytes));
        Assert.Contains(rows, row => row.TokenHash.SequenceEqual(oldHash) && row.ConsumedAt is not null);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Tokens);
        Assert.NotEqual(session.RefreshToken, result.Tokens.RefreshToken);
    }

    [Fact]
    public async Task RotateAsync_ExpiredTokenFailsWithoutSleeping()
    {
        var options = AuthenticationTestServices.CreateOptions();
        options.RefreshIdleLifetimeMinutes = 1;
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-11-14T12:00:00Z"));
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options, clock);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var account = await CreateAccountAsync(userManager);
        var tokenService = services.GetRequiredService<RefreshTokenService>();
        var session = await tokenService.CreateFamilyAsync(account, CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(2));

        var result = await tokenService.RotateAsync(session.RefreshToken, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task RotateAsync_ReusedTokenRevokesEntireFamily()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var account = await CreateAccountAsync(userManager);
        var tokenService = services.GetRequiredService<RefreshTokenService>();
        var session = await tokenService.CreateFamilyAsync(account, CancellationToken.None);
        var firstRotation = await tokenService.RotateAsync(session.RefreshToken, CancellationToken.None);

        var replay = await tokenService.RotateAsync(session.RefreshToken, CancellationToken.None);
        var family = await context.RefreshTokenFamilies.AsNoTracking().SingleAsync();
        var tokens = await context.RefreshTokens.AsNoTracking().ToListAsync();

        Assert.True(firstRotation.Succeeded);
        Assert.False(replay.Succeeded);
        Assert.NotNull(family.RevokedAt);
        Assert.All(tokens, token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task RotateAsync_SecurityStampChangeRevokesFamilyAndPasswordChangeInvalidatesSession()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var account = await CreateAccountAsync(userManager);
        var oldStamp = await userManager.GetSecurityStampAsync(account);
        var authService = services.GetRequiredService<AuthenticationService>();
        var tokenService = services.GetRequiredService<RefreshTokenService>();
        var session = await tokenService.CreateFamilyAsync(account, CancellationToken.None);

        var changed = await authService.ChangePasswordAsync(
            account.Id,
            TestPassword,
            "Changed-Test-Password-987!",
            CancellationToken.None);
        var newStamp = await userManager.GetSecurityStampAsync(account);
        var rotation = await tokenService.RotateAsync(session.RefreshToken, CancellationToken.None);
        var family = await context.RefreshTokenFamilies.AsNoTracking().SingleAsync();

        Assert.True(changed);
        Assert.NotEqual(oldStamp, newStamp);
        Assert.False(rotation.Succeeded);
        Assert.NotNull(family.RevokedAt);
    }

    [Fact]
    public async Task LogoutRevokesTheTokenFamily()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = AuthenticationTestServices.CreateIdentityServices(context);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var account = await CreateAccountAsync(userManager);
        var tokenService = services.GetRequiredService<RefreshTokenService>();
        var session = await tokenService.CreateFamilyAsync(account, CancellationToken.None);

        await tokenService.RevokeFamilyForTokenAsync(session.RefreshToken, CancellationToken.None);
        var family = await context.RefreshTokenFamilies.AsNoTracking().SingleAsync();
        var result = await tokenService.RotateAsync(session.RefreshToken, CancellationToken.None);

        Assert.NotNull(family.RevokedAt);
        Assert.False(result.Succeeded);
    }

    private const string TestPassword = "Test-Password-123!";

    private static async Task<KinTrafficIdentityAccount> CreateAccountAsync(
        UserManager<KinTrafficIdentityAccount> userManager)
    {
        var account = new KinTrafficIdentityAccount { UserName = "operator@example.invalid" };
        var result = await userManager.CreateAsync(account, TestPassword);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
        return account;
    }
}