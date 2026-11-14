using KinTraffic.Api.Persistence;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace KinTraffic.Api.Services.Security;

public sealed class CredentialVerificationService
{
    private readonly UserManager<KinTrafficIdentityAccount> _userManager;
    private readonly SignInManager<KinTrafficIdentityAccount> _signInManager;
    private readonly IPasswordHasher<KinTrafficIdentityAccount> _passwordHasher;
    private readonly KinTrafficIdentityAccount _dummyAccount;
    private readonly string _dummyPasswordHash;

    public CredentialVerificationService(
        UserManager<KinTrafficIdentityAccount> userManager,
        SignInManager<KinTrafficIdentityAccount> signInManager,
        IPasswordHasher<KinTrafficIdentityAccount> passwordHasher)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _passwordHasher = passwordHasher;
        _dummyAccount = new KinTrafficIdentityAccount { Id = Guid.NewGuid().ToString("N") };
        var dummyPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _dummyPasswordHash = _passwordHasher.HashPassword(_dummyAccount, dummyPassword);
    }

    public async Task<CredentialVerificationResult> VerifyAsync(string userName, string password)
    {
        return await VerifyAccountIdAsync(userName, password) is null
            ? CredentialVerificationResult.Failed
            : CredentialVerificationResult.Succeeded;
    }

    public async Task<string?> VerifyAccountIdAsync(string userName, string password)
    {
        var account = await _userManager.FindByNameAsync(userName);
        if (account is null)
        {
            VerifyDummyPassword(password);
            return null;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(account, password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            VerifyDummyPassword(password);
        }

        return result.Succeeded
            ? account.Id
            : null;
    }

    private void VerifyDummyPassword(string password) =>
        _passwordHasher.VerifyHashedPassword(_dummyAccount, _dummyPasswordHash, password);
}