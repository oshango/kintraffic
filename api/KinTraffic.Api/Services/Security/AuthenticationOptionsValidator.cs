using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KinTraffic.Api.Services.Security;

public sealed class AuthenticationOptionsValidator : IValidateOptions<AuthenticationOptions>
{
    private readonly IHostEnvironment _environment;

    public AuthenticationOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, AuthenticationOptions options)
    {
        var errors = new List<string>();

        Require(options.Issuer, "Authentication:Issuer", errors);
        Require(options.Audience, "Authentication:Audience", errors);
        RequirePositive(options.AccessTokenLifetimeMinutes, "Authentication:AccessTokenLifetimeMinutes", errors);
        RequireNonNegative(options.ClockSkewSeconds, "Authentication:ClockSkewSeconds", errors);
        RequirePositive(options.RefreshIdleLifetimeMinutes, "Authentication:RefreshIdleLifetimeMinutes", errors);
        RequirePositive(options.RefreshAbsoluteLifetimeDays, "Authentication:RefreshAbsoluteLifetimeDays", errors);
        Require(options.RefreshCookieName, "Authentication:RefreshCookieName", errors);
        Require(options.RefreshCookieSameSite, "Authentication:RefreshCookieSameSite", errors);
        Require(options.RefreshCookiePath, "Authentication:RefreshCookiePath", errors);
        Require(options.SigningKeyId, "Authentication:SigningKeyId", errors);
        Require(options.SigningPrivateKeyPem, "Authentication:SigningPrivateKeyPem", errors);
        RequirePositive(options.SourceIpPermitLimit, "Authentication:SourceIpPermitLimit", errors);
        RequirePositive(options.SourceIpWindowSeconds, "Authentication:SourceIpWindowSeconds", errors);
        RequirePositive(options.AccountPermitLimit, "Authentication:AccountPermitLimit", errors);
        RequirePositive(options.AccountWindowSeconds, "Authentication:AccountWindowSeconds", errors);
        if (options.MaxAccountThrottlePartitions != 10000)
        {
            errors.Add("Authentication:MaxAccountThrottlePartitions must be 10000.");
        }

        if (options.AccountThrottleIdleMinutes != 30)
        {
            errors.Add("Authentication:AccountThrottleIdleMinutes must be 30.");
        }

        if (options.AccountWindowSeconds is int accountWindowSeconds &&
            options.AccountThrottleIdleMinutes is int accountThrottleIdleMinutes &&
            accountThrottleIdleMinutes * 60L < accountWindowSeconds)
        {
            errors.Add("Authentication:AccountThrottleIdleMinutes must be at least as long as AccountWindowSeconds.");
        }
        if (options.MaxFailedAccessAttempts != 10)
        {
            errors.Add("Authentication:MaxFailedAccessAttempts must be 10.");
        }

        if (options.LockoutDurationMinutes != 5)
        {
            errors.Add("Authentication:LockoutDurationMinutes must be 5.");
        }

        if (options.PasswordMinimumLength is null or < 15)
        {
            errors.Add("Authentication:PasswordMinimumLength must be at least 15.");
        }

        if (options.PasswordMaximumLength != 128 ||
            options.PasswordMaximumLength < options.PasswordMinimumLength)
        {
            errors.Add("Authentication:PasswordMaximumLength must be 128 and at least the minimum length.");
        }

        if (options.PasswordHasherIterationCount is null or < 220000)
        {
            errors.Add("Authentication:PasswordHasherIterationCount must be at least 220000.");
        }

        if (options.RefreshIdleLifetimeMinutes is int idleMinutes &&
            options.RefreshAbsoluteLifetimeDays is int absoluteDays &&
            absoluteDays * 24L * 60L < idleMinutes)
        {
            errors.Add("Authentication:RefreshAbsoluteLifetimeDays must be at least the idle lifetime.");
        }

        if (options.RefreshCookieSameSite is not null &&
            !new[] { "Strict", "Lax", "None" }.Contains(options.RefreshCookieSameSite, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("Authentication:RefreshCookieSameSite must be Strict, Lax, or None.");
        }

        if (options.RefreshCookiePath is not null && !options.RefreshCookiePath.StartsWith('/'))
        {
            errors.Add("Authentication:RefreshCookiePath must start with '/'.");
        }

        if (options.RefreshCookieName?.StartsWith("__Host-", StringComparison.Ordinal) == true &&
            options.RefreshCookiePath is not null && options.RefreshCookiePath != "/")
        {
            errors.Add("Authentication:RefreshCookiePath must be '/' for a __Host- refresh cookie.");
        }

        if (options.AllowedOrigins is null || options.AllowedOrigins.Length == 0)
        {
            if (!_environment.IsDevelopment())
            {
                errors.Add("Authentication:AllowedOrigins must contain exact origins outside Development.");
            }
        }
        else
        {
            foreach (var origin in options.AllowedOrigins)
            {
                if (!IsExactOrigin(origin, requireHttps: !_environment.IsDevelopment()))
                {
                    errors.Add("Authentication:AllowedOrigins must contain exact origins without user-info, paths, or wildcards; non-Development origins must use HTTPS.");
                    break;
                }
            }
        }

        if (_environment.IsProduction() && options.AllowedOrigins?.Length != 1)
        {
            errors.Add("Authentication:AllowedOrigins must contain exactly one origin in Production.");
        }

        if (options.TrustedProxies is null || options.TrustedProxies.Length == 0)
        {
            errors.Add("Authentication:TrustedProxies must be explicitly configured with IP addresses or 'none'.");
        }
        else
        {
            var explicitlyNoProxies = options.TrustedProxies.Contains("none", StringComparer.OrdinalIgnoreCase);
            if ((explicitlyNoProxies && options.TrustedProxies.Length != 1) ||
                (!explicitlyNoProxies && options.TrustedProxies.Any(proxy => !IPAddress.TryParse(proxy, out _))))
            {
                errors.Add("Authentication:TrustedProxies must contain exact IP addresses, or only 'none'.");
            }
        }

        ValidateSigningKeys(options, errors);
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private void ValidateSigningKeys(AuthenticationOptions options, List<string> errors)
    {
        var minimumKeySize = _environment.IsProduction() ? 3072 : 2048;
        if (!string.IsNullOrWhiteSpace(options.SigningPrivateKeyPem))
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(options.SigningPrivateKeyPem);
                if (rsa.KeySize < minimumKeySize || rsa.ExportParameters(true).D is null)
                {
                    errors.Add($"Authentication:SigningPrivateKeyPem must be an RSA private key of at least {minimumKeySize} bits.");
                }
            }
            catch (Exception exception) when (exception is CryptographicException or ArgumentException)
            {
                errors.Add("Authentication:SigningPrivateKeyPem must contain a valid RSA private key.");
            }
        }

        var keyIds = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(options.SigningKeyId))
        {
            keyIds.Add(options.SigningKeyId);
        }

        foreach (var previousKey in options.PreviousSigningKeys ?? [])
        {
            if (string.IsNullOrWhiteSpace(previousKey.KeyId) || !keyIds.Add(previousKey.KeyId))
            {
                errors.Add("Authentication:PreviousSigningKeys must have unique, non-empty key IDs.");
            }

            if (string.IsNullOrWhiteSpace(previousKey.PublicKeyPem))
            {
                errors.Add("Authentication:PreviousSigningKeys entries must include a public key.");
                continue;
            }

            if (previousKey.PublicKeyPem.Contains("PRIVATE KEY", StringComparison.Ordinal))
            {
                errors.Add("Authentication:PreviousSigningKeys must contain public keys only.");
                continue;
            }

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(previousKey.PublicKeyPem);
                if (rsa.KeySize < minimumKeySize)
                {
                    errors.Add($"Authentication:PreviousSigningKeys must use RSA keys of at least {minimumKeySize} bits.");
                }
            }
            catch (Exception exception) when (exception is CryptographicException or ArgumentException)
            {
                errors.Add("Authentication:PreviousSigningKeys must contain valid RSA public keys.");
            }
        }
    }

    private static bool IsExactOrigin(string origin, bool requireHttps) =>
        !origin.Contains('*') &&
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        (!requireHttps || uri.Scheme == Uri.UriSchemeHttps) &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        uri.Query.Length == 0 &&
        uri.Fragment.Length == 0 &&
        string.Equals(uri.GetLeftPart(UriPartial.Authority), origin, StringComparison.Ordinal);

    private static void Require(string? value, string key, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{key} is required.");
        }
    }

    private static void RequirePositive(int? value, string key, List<string> errors)
    {
        if (value is null or <= 0)
        {
            errors.Add($"{key} must be greater than zero.");
        }
    }

    private static void RequireNonNegative(int? value, string key, List<string> errors)
    {
        if (value is null or < 0)
        {
            errors.Add($"{key} must be zero or greater.");
        }
    }
}