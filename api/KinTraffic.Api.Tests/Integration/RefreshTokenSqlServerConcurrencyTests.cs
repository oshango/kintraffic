using KinTraffic.Api.Persistence;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KinTraffic.Api.Tests.Integration;

public class RefreshTokenSqlServerConcurrencyTests
{
    [SqlServerFact]
    public async Task ConcurrentRotation_AllowsOneRedemptionAndRevokesFamilyOnReplay()
    {
        var connectionString = Environment.GetEnvironmentVariable("KINTRAFFIC_SQLSERVER_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var options = AuthenticationTestServices.CreateOptions();
        string originalRefreshToken;
        Guid familyId;
        await using (var setupContext = CreateContext(connectionString))
        {
            using var setupServices = AuthenticationTestServices.CreateIdentityServices(setupContext, options);
            var userManager = setupServices.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
            var account = new KinTrafficIdentityAccount { UserName = $"sql-test-{Guid.NewGuid():N}@example.invalid" };
            var created = await userManager.CreateAsync(account, "Test-Password-123!");
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
            var initialSession = await setupServices.GetRequiredService<RefreshTokenService>()
                .CreateFamilyAsync(account, CancellationToken.None);
            originalRefreshToken = initialSession.RefreshToken;
            familyId = await setupContext.RefreshTokenFamilies
                .Where(family => family.AccountId == account.Id)
                .Select(family => family.Id)
                .SingleAsync();
        }

        async Task<RefreshRotationResult> RedeemAsync()
        {
            await using var context = CreateContext(connectionString);
            using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
            return await services.GetRequiredService<RefreshTokenService>()
                .RotateAsync(originalRefreshToken, CancellationToken.None);
        }

        var results = await Task.WhenAll(RedeemAsync(), RedeemAsync());

        Assert.Single(results, result => result.Succeeded);
        Assert.Single(results, result => !result.Succeeded);
        await using var verifyContext = CreateContext(connectionString);
        var family = await verifyContext.RefreshTokenFamilies.SingleAsync(item => item.Id == familyId);
        Assert.NotNull(family.RevokedAt);
    }

    private static KinTrafficDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<KinTrafficDbContext>()
            .UseSqlServer(connectionString)
            .Options);
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KINTRAFFIC_SQLSERVER_TEST_CONNECTION")))
        {
            Skip = "Set KINTRAFFIC_SQLSERVER_TEST_CONNECTION to a disposable migrated SQL Server database.";
        }
    }
}