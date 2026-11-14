using System.Security.Cryptography;
using KinTraffic.Api.Persistence;
using KinTraffic.Api.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KinTraffic.Api.Tests.TestHelpers;

internal static class AuthenticationTestServices
{
    public static AuthenticationOptions CreateOptions()
    {
        using var rsa = RSA.Create(2048);
        return new AuthenticationOptions
        {
            Issuer = "https://issuer.test",
            Audience = "kintraffic-api-test",
            AccessTokenLifetimeMinutes = 5,
            ClockSkewSeconds = 0,
            RefreshIdleLifetimeMinutes = 30,
            RefreshAbsoluteLifetimeDays = 1,
            RefreshCookieName = "__Host-test-refresh",
            RefreshCookieSameSite = "Strict",
            RefreshCookiePath = "/",
            AllowedOrigins = ["https://ui.test"],
            TrustedProxies = ["none"],
            SigningKeyId = "test-current",
            SigningPrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
            SourceIpPermitLimit = 3,
            SourceIpWindowSeconds = 60,
            AccountPermitLimit = 2,
            AccountWindowSeconds = 60,
            MaxAccountThrottlePartitions = 32,
            AccountThrottleIdleMinutes = 5,
            MaxFailedAccessAttempts = 2,
            LockoutDurationMinutes = 5
        };
    }

    public static ServiceProvider CreateIdentityServices(
        KinTrafficDbContext context,
        AuthenticationOptions? options = null,
        TimeProvider? timeProvider = null,
        ILoggerProvider? loggerProvider = null)
    {
        options ??= CreateOptions();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            if (loggerProvider is not null)
            {
                builder.AddProvider(loggerProvider);
            }
        });
        services.AddSingleton(context);
        services.AddSingleton(options);
        services.AddSingleton(timeProvider ?? TimeProvider.System);
        services.AddSingleton(new SigningKeyRing(options));
        services.AddAuthentication();
        services.AddIdentityCore<KinTrafficIdentityAccount>(identityOptions =>
        {
            identityOptions.Lockout.AllowedForNewUsers = true;
            identityOptions.Lockout.MaxFailedAccessAttempts = options.MaxFailedAccessAttempts!.Value;
            identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(options.LockoutDurationMinutes!.Value);
        })
            .AddSignInManager()
            .AddEntityFrameworkStores<KinTrafficDbContext>();
        services.AddScoped<CredentialVerificationService>();
        services.AddScoped<AccessTokenService>();
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<AuthenticationService>();
        services.AddSingleton<AccountAttemptLimiter>();
        return services.BuildServiceProvider();
    }
}

internal sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);
}

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_messages);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add(formatter(state, exception));
        }
    }
}