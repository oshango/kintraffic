using KinTraffic.Api.Services.Security;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Security;

public class DevelopmentAccountProvisioningTests
{
    [Fact]
    public void EnsureAllowed_RequiresExplicitCommandFlag()
    {
        var environment = new TestHostEnvironment(Environments.Development);

        Assert.Throws<InvalidOperationException>(() =>
            DevelopmentAccountProvisioning.EnsureAllowed(environment, []));
    }

    [Fact]
    public void EnsureAllowed_RefusesProductionEvenWhenFlagIsPresent()
    {
        var environment = new TestHostEnvironment(Environments.Production);

        Assert.Throws<InvalidOperationException>(() =>
            DevelopmentAccountProvisioning.EnsureAllowed(
                environment,
                [DevelopmentAccountProvisioning.CommandFlag]));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}