using KinTraffic.Api.Persistence;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class CredentialVerificationServiceTests
{
    [Fact]
    public async Task PasswordPolicy_EnforcesLengthsWithoutCompositionRulesAndUsesConfiguredPbkdf2()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var options = AuthenticationTestServices.CreateOptions();
        using var services = AuthenticationTestServices.CreateIdentityServices(context, options);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        var password = new string('a', options.PasswordMinimumLength!.Value);
        var account = new KinTrafficIdentityAccount { UserName = "policy@example.invalid" };

        var tooShort = await userManager.CreateAsync(new KinTrafficIdentityAccount { UserName = "short@example.invalid" }, new string('a', 14));
        var tooLong = await userManager.CreateAsync(new KinTrafficIdentityAccount { UserName = "long@example.invalid" }, new string('a', 129));
        var accepted = await userManager.CreateAsync(account, password);
        var maximumLength = await userManager.CreateAsync(new KinTrafficIdentityAccount { UserName = "max@example.invalid" }, new string('a', 128));

        Assert.False(tooShort.Succeeded);
        Assert.Contains(tooLong.Errors, error => error.Code == "PasswordTooLong");
        Assert.True(accepted.Succeeded, string.Join(", ", accepted.Errors.Select(error => error.Description)));
        Assert.True(maximumLength.Succeeded, string.Join(", ", maximumLength.Errors.Select(error => error.Description)));
        Assert.True(await userManager.CheckPasswordAsync(account, password));
        var bytes = Convert.FromBase64String(account.PasswordHash!);
        Assert.Equal(1, bytes[0]);
        Assert.Equal(2, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(1, 4)));
        Assert.Equal(options.PasswordHasherIterationCount, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(5, 4)));
    }

    [Fact]
    public async Task VerifyAsync_ValidCredentials_Succeeds()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = CreateIdentityServices(context);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        const string userName = "operator@example.invalid";
        var password = CreatePassword();
        await CreateAccountAsync(userManager, userName, password);

        var result = await services.GetRequiredService<CredentialVerificationService>()
            .VerifyAsync(userName, password);

        Assert.Equal(CredentialVerificationResult.Succeeded, result);
    }

    [Fact]
    public async Task VerifyAsync_InvalidPassword_ReturnsGenericFailure()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = CreateIdentityServices(context);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        const string userName = "operator@example.invalid";
        await CreateAccountAsync(userManager, userName, CreatePassword());

        var result = await services.GetRequiredService<CredentialVerificationService>()
            .VerifyAsync(userName, "Incorrect-password-123!");

        Assert.Equal(CredentialVerificationResult.Failed, result);
    }

    [Fact]
    public async Task VerifyAsync_UnknownAccount_ReturnsGenericFailure()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = CreateIdentityServices(context);

        var result = await services.GetRequiredService<CredentialVerificationService>()
            .VerifyAsync("unknown@example.invalid", "Incorrect-password-123!");

        Assert.Equal(CredentialVerificationResult.Failed, result);
    }

    [Fact]
    public async Task VerifyAsync_LockedOutAccount_ReturnsGenericFailure()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = CreateIdentityServices(context);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        const string userName = "operator@example.invalid";
        var password = CreatePassword();
        var account = await CreateAccountAsync(userManager, userName, password);
        await userManager.SetLockoutEnabledAsync(account, true);
        await userManager.SetLockoutEndDateAsync(account, DateTimeOffset.UtcNow.AddMinutes(5));

        var result = await services.GetRequiredService<CredentialVerificationService>()
            .VerifyAsync(userName, password);

        Assert.Equal(CredentialVerificationResult.Failed, result);
    }

    [Fact]
    public async Task VerifyAsync_LockoutTriggersAtConfiguredAttemptCount_AndMatchesBadPasswordResult()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var services = CreateIdentityServices(context);
        var userManager = services.GetRequiredService<UserManager<KinTrafficIdentityAccount>>();
        const string userName = "operator@example.invalid";
        var password = CreatePassword();
        await CreateAccountAsync(userManager, userName, password);
        var verifier = services.GetRequiredService<CredentialVerificationService>();

        var badPasswordResult = CredentialVerificationResult.Failed;
        for (var attempt = 0; attempt < 9; attempt++)
        {
            badPasswordResult = await verifier.VerifyAsync(userName, "Incorrect-password-123!");
        }

        var accountBeforeLockout = await userManager.FindByNameAsync(userName);
        Assert.False(await userManager.IsLockedOutAsync(accountBeforeLockout!));
        var lockoutResult = await verifier.VerifyAsync(userName, "Incorrect-password-123!");
        var correctPasswordDuringLockout = await verifier.VerifyAsync(userName, password);
        var account = await userManager.FindByNameAsync(userName);

        Assert.Equal(CredentialVerificationResult.Failed, badPasswordResult);
        Assert.Equal(badPasswordResult, lockoutResult);
        Assert.Equal(badPasswordResult, correctPasswordDuringLockout);
        Assert.True(await userManager.IsLockedOutAsync(account!));
    }

    private static ServiceProvider CreateIdentityServices(KinTrafficDbContext context)
        => AuthenticationTestServices.CreateIdentityServices(context);

    private static async Task<KinTrafficIdentityAccount> CreateAccountAsync(
        UserManager<KinTrafficIdentityAccount> userManager,
        string userName,
        string password)
    {
        var account = new KinTrafficIdentityAccount { UserName = userName };
        var result = await userManager.CreateAsync(account, password);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
        return account;
    }

    private static string CreatePassword() => $"Test-{Guid.NewGuid():N}aA1!";
}