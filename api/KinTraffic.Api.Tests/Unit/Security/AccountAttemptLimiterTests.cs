using System.Net;
using KinTraffic.Api.Services.Security;
using KinTraffic.Api.Tests.TestHelpers;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class AccountAttemptLimiterTests
{
    [Fact]
    public void TryAcquire_LimitsSameNormalizedAccountAndIp_WithoutAccountLookup()
    {
        var options = AuthenticationTestServices.CreateOptions();
        var limiter = new AccountAttemptLimiter(TimeProvider.System, options);
        var clientIp = IPAddress.Parse("192.0.2.10");

        Assert.True(limiter.TryAcquire(clientIp, "EXISTING@EXAMPLE.INVALID"));
        Assert.True(limiter.TryAcquire(clientIp, "EXISTING@EXAMPLE.INVALID"));
        Assert.False(limiter.TryAcquire(clientIp, "EXISTING@EXAMPLE.INVALID"));

        Assert.True(limiter.TryAcquire(clientIp, "UNKNOWN@EXAMPLE.INVALID"));
        Assert.True(limiter.TryAcquire(clientIp, "UNKNOWN@EXAMPLE.INVALID"));
        Assert.False(limiter.TryAcquire(clientIp, "UNKNOWN@EXAMPLE.INVALID"));
    }

    [Fact]
    public void TryAcquire_SeparatesClients_AndRejectsNewKeysWhenPartitionCapacityIsFull()
    {
        var options = AuthenticationTestServices.CreateOptions();
        options.MaxAccountThrottlePartitions = 1;
        var limiter = new AccountAttemptLimiter(TimeProvider.System, options);

        Assert.True(limiter.TryAcquire(IPAddress.Parse("192.0.2.10"), "OPERATOR"));
        Assert.False(limiter.TryAcquire(IPAddress.Parse("192.0.2.11"), "OPERATOR"));
        Assert.True(limiter.TryAcquire(IPAddress.Parse("192.0.2.10"), "OPERATOR"));
        Assert.False(limiter.TryAcquire(IPAddress.Parse("192.0.2.10"), "OPERATOR"));
    }

    [Fact]
    public void TryAcquire_EvictsIdlePartitions()
    {
        var options = AuthenticationTestServices.CreateOptions();
        options.MaxAccountThrottlePartitions = 1;
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-11-14T12:00:00Z"));
        var limiter = new AccountAttemptLimiter(clock, options);

        Assert.True(limiter.TryAcquire(IPAddress.Parse("192.0.2.10"), "FIRST"));
        clock.Advance(TimeSpan.FromMinutes(6));

        Assert.True(limiter.TryAcquire(IPAddress.Parse("192.0.2.11"), "SECOND"));
    }
}