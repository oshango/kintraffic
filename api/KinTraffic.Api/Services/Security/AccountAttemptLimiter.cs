using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace KinTraffic.Api.Services.Security;

public sealed class AccountAttemptLimiter
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Partition> _partitions = new(StringComparer.Ordinal);
    private readonly byte[] _partitionKey = RandomNumberGenerator.GetBytes(32);
    private readonly TimeProvider _timeProvider;
    private readonly AuthenticationOptions _options;

    public AccountAttemptLimiter(TimeProvider timeProvider, AuthenticationOptions options)
    {
        _timeProvider = timeProvider;
        _options = options;
    }

    public bool TryAcquire(IPAddress? remoteIpAddress, string normalizedUserName)
        => TryAcquire(remoteIpAddress, normalizedUserName, out _);

    public bool TryAcquire(IPAddress? remoteIpAddress, string normalizedUserName, out TimeSpan retryAfter)
    {
        var clientIp = remoteIpAddress?.MapToIPv6().ToString() ?? "unknown";
        var input = Encoding.UTF8.GetBytes($"{clientIp}\0{normalizedUserName}");
        var partition = Convert.ToHexString(HMACSHA256.HashData(_partitionKey, input));
        CryptographicOperations.ZeroMemory(input);
        var now = _timeProvider.GetUtcNow();
        var window = TimeSpan.FromSeconds(_options.AccountWindowSeconds!.Value);
        var idleLimit = TimeSpan.FromMinutes(_options.AccountThrottleIdleMinutes!.Value);

        lock (_sync)
        {
            foreach (var idleKey in _partitions
                         .Where(pair => now - pair.Value.LastSeenAt >= idleLimit)
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _partitions.Remove(idleKey);
            }

            if (!_partitions.TryGetValue(partition, out var state))
            {
                if (_partitions.Count >= _options.MaxAccountThrottlePartitions!.Value)
                {
                    retryAfter = Max(TimeSpan.FromSeconds(1),
                        _partitions.MinBy(pair => pair.Value.LastSeenAt).Value.LastSeenAt + idleLimit - now);
                    return false;
                }

                state = new Partition(now);
                _partitions.Add(partition, state);
            }

            state.LastSeenAt = now;
            if (now - state.WindowStartedAt >= window)
            {
                state.WindowStartedAt = now;
                state.Attempts = 0;
            }

            if (state.Attempts >= _options.AccountPermitLimit!.Value)
            {
                retryAfter = Max(TimeSpan.FromSeconds(1), state.WindowStartedAt + window - now);
                return false;
            }

            state.Attempts++;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;

    private sealed class Partition(DateTimeOffset now)
    {
        public DateTimeOffset WindowStartedAt { get; set; } = now;
        public DateTimeOffset LastSeenAt { get; set; } = now;
        public int Attempts { get; set; }
    }
}