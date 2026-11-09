using KinTraffic.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KinTraffic.Api.Tests.Unit.Controllers;

public class RouteCompatibilityTests
{
    [Theory]
    [InlineData(typeof(AssetController), "api/[controller]", "api/assets")]
    [InlineData(typeof(FaultController), "api/[controller]", "api/faults")]
    public void Controller_ExposesLegacyAndPluralRoutes(Type controllerType, params string[] expectedRoutes)
    {
        var routeTemplates = controllerType.GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(route => route.Template);

        foreach (var expectedRoute in expectedRoutes)
        {
            Assert.Contains(expectedRoute, routeTemplates);
        }
    }
}
