using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace KinTraffic.Api.Services.Security;

public static class AuthenticationRateLimiting
{
    public const string PolicyName = "Authentication";

    public static bool AddAuthenticationRateLimiting(
        this IServiceCollection services,
        AuthenticationOptions options)
    {
        if (options.SourceIpPermitLimit is null or <= 0 || options.SourceIpWindowSeconds is null or <= 0)
        {
            throw new InvalidOperationException(
                $"{AuthenticationOptions.SectionName}:SourceIpPermitLimit and SourceIpWindowSeconds are required.");
        }

        var trustedProxies = ReadTrustedProxies(options.TrustedProxies);
        if (trustedProxies.Length > 0)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
                options.ForwardLimit = 1;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                foreach (var proxy in trustedProxies)
                {
                    options.KnownProxies.Add(proxy);
                }
            });
        }

        var limiterWindow = TimeSpan.FromSeconds(options.SourceIpWindowSeconds!.Value);
        var permitLimit = options.SourceIpPermitLimit!.Value;

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(PolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientIpPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = permitLimit,
                        QueueLimit = 0,
                        Window = limiterWindow
                    }));

            options.OnRejected = async (rejection, cancellationToken) =>
            {
                var response = rejection.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.ContentType = "application/json";

                var retryAfterSeconds = rejection.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out var retryAfter)
                    ? Math.Max(1, (long)Math.Ceiling(retryAfter.TotalSeconds))
                    : (long)Math.Ceiling(limiterWindow.TotalSeconds);
                response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

                await response.WriteAsJsonAsync(
                    new { error = "rate_limit_exceeded" },
                    cancellationToken);
            };
        });

        return trustedProxies.Length > 0;
    }

    public static string GetClientIpPartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown";

    private static IPAddress[] ReadTrustedProxies(string[]? configuredAddresses)
    {
        if (configuredAddresses is null || configuredAddresses.Length == 0 ||
            (configuredAddresses.Length == 1 && string.Equals(configuredAddresses[0], "none", StringComparison.OrdinalIgnoreCase)))
        {
            return [];
        }

        var trustedProxies = new List<IPAddress>(configuredAddresses.Length);

        foreach (var address in configuredAddresses)
        {
            if (!IPAddress.TryParse(address, out var proxy))
            {
                throw new InvalidOperationException(
                    $"Configuration '{AuthenticationOptions.SectionName}:TrustedProxies' must contain only valid proxy IP addresses.");
            }

            trustedProxies.Add(proxy);
        }

        return trustedProxies.Distinct().ToArray();
    }
}