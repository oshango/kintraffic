using System.Security.Cryptography;
using System.Text;
using KinTraffic.Api.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace KinTraffic.Api.Services.Security;

public sealed record AuthenticationSessionTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record RefreshRotationResult(bool Succeeded, AuthenticationSessionTokens? Tokens)
{
    public static RefreshRotationResult Failed { get; } = new(false, null);
}

public sealed class RefreshTokenService
{
    private readonly KinTrafficDbContext _dbContext;
    private readonly UserManager<KinTrafficIdentityAccount> _userManager;
    private readonly AuthenticationOptions _options;
    private readonly AccessTokenService _accessTokens;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenService(
        KinTrafficDbContext dbContext,
        UserManager<KinTrafficIdentityAccount> userManager,
        AuthenticationOptions options,
        AccessTokenService accessTokens,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _options = options;
        _accessTokens = accessTokens;
        _timeProvider = timeProvider;
    }

    public async Task<AuthenticationSessionTokens> CreateFamilyAsync(
        KinTrafficIdentityAccount account,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var absoluteExpiresAt = now.AddDays(_options.RefreshAbsoluteLifetimeDays!.Value);
        var securityStamp = await _userManager.GetSecurityStampAsync(account);
        var family = new RefreshTokenFamily
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            SecurityStampHash = Hash(Encoding.UTF8.GetBytes(securityStamp)),
            CreatedAt = now,
            AbsoluteExpiresAt = absoluteExpiresAt,
            ConcurrencyToken = Guid.NewGuid()
        };
        var refreshToken = CreateRefreshToken(family.Id, now, absoluteExpiresAt);
        var accessToken = _accessTokens.Issue(account.Id);

        _dbContext.RefreshTokenFamilies.Add(family);
        _dbContext.RefreshTokens.Add(refreshToken.Record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticationSessionTokens(
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken.Value,
            refreshToken.Record.ExpiresAt);
    }

    public async Task<RefreshRotationResult> RotateAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (!TryHashRefreshToken(refreshToken, out var tokenHash))
        {
            return RefreshRotationResult.Failed;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var token = await _dbContext.RefreshTokens
            .AsNoTracking()
            .Include(record => record.Family)
            .SingleOrDefaultAsync(record => record.TokenHash == tokenHash, cancellationToken);
        if (token is null)
        {
            return RefreshRotationResult.Failed;
        }

        var now = _timeProvider.GetUtcNow();
        if (token.ConsumedAt is not null)
        {
            await RevokeFamilyInTransactionAsync(token.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RefreshRotationResult.Failed;
        }

        if (token.RevokedAt is not null ||
            token.Family.RevokedAt is not null ||
            token.ExpiresAt <= now ||
            token.Family.AbsoluteExpiresAt <= now)
        {
            return RefreshRotationResult.Failed;
        }

        var account = await _userManager.FindByIdAsync(token.Family.AccountId);
        if (account is null || await _userManager.IsLockedOutAsync(account))
        {
            await RevokeFamilyInTransactionAsync(token.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RefreshRotationResult.Failed;
        }

        var currentStamp = await _userManager.GetSecurityStampAsync(account);
        var currentStampHash = Hash(Encoding.UTF8.GetBytes(currentStamp));
        if (!CryptographicOperations.FixedTimeEquals(token.Family.SecurityStampHash, currentStampHash))
        {
            await RevokeFamilyInTransactionAsync(token.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RefreshRotationResult.Failed;
        }

        var familyConcurrencyToken = Guid.NewGuid();
        var claimedFamily = await _dbContext.RefreshTokenFamilies
            .Where(family => family.Id == token.FamilyId &&
                             family.ConcurrencyToken == token.Family.ConcurrencyToken &&
                             family.RevokedAt == null &&
                             family.AbsoluteExpiresAt > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(family => family.ConcurrencyToken, familyConcurrencyToken), cancellationToken);

        if (claimedFamily != 1)
        {
            var currentRecord = await _dbContext.RefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(record => record.Id == token.Id, cancellationToken);
            if (currentRecord?.ConsumedAt is not null)
            {
                await RevokeFamilyInTransactionAsync(token.FamilyId, now, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return RefreshRotationResult.Failed;
        }

        var nextConcurrencyToken = Guid.NewGuid();
        var redeemed = await _dbContext.RefreshTokens
            .Where(record => record.Id == token.Id &&
                             record.ConcurrencyToken == token.ConcurrencyToken &&
                             record.ConsumedAt == null &&
                             record.RevokedAt == null &&
                             record.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(record => record.ConsumedAt, now)
                .SetProperty(record => record.ConcurrencyToken, nextConcurrencyToken), cancellationToken);

        if (redeemed != 1)
        {
            var currentRecord = await _dbContext.RefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(record => record.Id == token.Id, cancellationToken);
            if (currentRecord?.ConsumedAt is not null)
            {
                await RevokeFamilyInTransactionAsync(token.FamilyId, now, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return RefreshRotationResult.Failed;
        }

        var next = CreateRefreshToken(token.FamilyId, now, token.Family.AbsoluteExpiresAt);
        _dbContext.RefreshTokens.Add(next.Record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var accessToken = _accessTokens.Issue(account.Id);
        return new RefreshRotationResult(true, new AuthenticationSessionTokens(
            accessToken.Value,
            accessToken.ExpiresAt,
            next.Value,
            next.Record.ExpiresAt));
    }

    public async Task RevokeFamilyForTokenAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (!TryHashRefreshToken(refreshToken, out var tokenHash))
        {
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var familyId = await _dbContext.RefreshTokens
            .Where(record => record.TokenHash == tokenHash)
            .Select(record => (Guid?)record.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);
        if (familyId is not null)
        {
            await RevokeFamilyInTransactionAsync(familyId.Value, _timeProvider.GetUtcNow(), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RevokeAllForAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var concurrencyToken = Guid.NewGuid();
        var familyIds = await _dbContext.RefreshTokenFamilies
            .Where(family => family.AccountId == accountId && family.RevokedAt == null)
            .Select(family => family.Id)
            .ToArrayAsync(cancellationToken);
        await _dbContext.RefreshTokenFamilies
            .Where(family => familyIds.Contains(family.Id))
            .ExecuteUpdateAsync(update => update
                .SetProperty(family => family.RevokedAt, now)
                .SetProperty(family => family.ConcurrencyToken, concurrencyToken), cancellationToken);
        await _dbContext.RefreshTokens
            .Where(token => token.RevokedAt == null && familyIds.Contains(token.FamilyId))
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RevokeFamilyInTransactionAsync(
        Guid familyId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _dbContext.RefreshTokenFamilies
            .Where(family => family.Id == familyId && family.RevokedAt == null)
            .ExecuteUpdateAsync(update => update
                .SetProperty(family => family.RevokedAt, now)
                .SetProperty(family => family.ConcurrencyToken, Guid.NewGuid()), cancellationToken);
        await _dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    private (string Value, RefreshTokenRecord Record) CreateRefreshToken(
        Guid familyId,
        DateTimeOffset now,
        DateTimeOffset absoluteExpiresAt)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var value = WebEncoders.Base64UrlEncode(bytes);
        var expiresAt = Min(now.AddMinutes(_options.RefreshIdleLifetimeMinutes!.Value), absoluteExpiresAt);
        var record = new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            TokenHash = Hash(bytes),
            CreatedAt = now,
            ExpiresAt = expiresAt,
            ConcurrencyToken = Guid.NewGuid()
        };
        CryptographicOperations.ZeroMemory(bytes);
        return (value, record);
    }

    private static bool TryHashRefreshToken(string? value, out byte[] hash)
    {
        hash = [];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var bytes = WebEncoders.Base64UrlDecode(value);
            if (bytes.Length != 32)
            {
                CryptographicOperations.ZeroMemory(bytes);
                return false;
            }

            hash = Hash(bytes);
            CryptographicOperations.ZeroMemory(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Hash(byte[] value) => SHA256.HashData(value);

    private static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second) => first < second ? first : second;
}