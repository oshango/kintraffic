using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KinTraffic.Api.OpenApi;

public static class DevelopmentOpenApiExtensions
{
    public static IServiceCollection AddKinTrafficOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }

    public static void MapOpenApiInDevelopment(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
    }
}