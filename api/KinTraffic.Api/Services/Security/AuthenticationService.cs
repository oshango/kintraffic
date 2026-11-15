using System.Net;
using KinTraffic.Api.Persistence;
using Microsoft.AspNetCore.Identity;

namespace KinTraffic.Api.Services.Security;

public enum LoginAttemptResult
{
    Failed,
    RateLimited,
    Succeeded
}

public sealed record LoginResult(
    LoginAttemptResult Result,
    AuthenticationSessionTokens? Tokens,
    TimeSpan? RetryAfter = null);

public sealed class AuthenticationService
{
    private readonly UserManager<KinTrafficIdentityAccount> _userManager;
    private readonly CredentialVerificationService _credentials;
    private readonly AccountAttemptLimiter _accountLimiter;
    private readonly RefreshTokenService _refreshTokens;

    public AuthenticationService(
        UserManager<KinTrafficIdentityAccount> userManager,
        CredentialVerificationService credentials,
        AccountAttemptLimiter accountLimiter,
        RefreshTokenService refreshTokens)
    {
        _userManager = userManager;
        _credentials = credentials;
        _accountLimiter = accountLimiter;
        _refreshTokens = refreshTokens;
    }

    public async Task<LoginResult> LoginAsync(
        string userName,
        string password,
        IPAddress? remoteIpAddress,
        CancellationToken cancellationToken)
    {
        var normalizedUserName = _userManager.NormalizeName(userName);
        if (string.IsNullOrEmpty(normalizedUserName))
        {
            return new LoginResult(LoginAttemptResult.Failed, null);
        }

        if (!_accountLimiter.TryAcquire(remoteIpAddress, normalizedUserName, out var retryAfter))
        {
            return new LoginResult(LoginAttemptResult.RateLimited, null, retryAfter);
        }

        var accountId = await _credentials.VerifyAccountIdAsync(userName, password);
        if (accountId is null)
        {
            return new LoginResult(LoginAttemptResult.Failed, null);
        }

        var account = await _userManager.FindByIdAsync(accountId);
        if (account is null || await _userManager.IsLockedOutAsync(account))
        {
            return new LoginResult(LoginAttemptResult.Failed, null);
        }

        return new LoginResult(
            LoginAttemptResult.Succeeded,
            await _refreshTokens.CreateFamilyAsync(account, cancellationToken));
    }

    public Task<RefreshRotationResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken) =>
        _refreshTokens.RotateAsync(refreshToken, cancellationToken);

    public Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken) =>
        _refreshTokens.RevokeFamilyForTokenAsync(refreshToken, cancellationToken);

    public async Task<bool> ChangePasswordAsync(
        string accountId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var account = await _userManager.FindByIdAsync(accountId);
        if (account is null)
        {
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(account, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return false;
        }

        await _refreshTokens.RevokeAllForAccountAsync(account.Id, cancellationToken);
        return true;
    }
}