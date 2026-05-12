using AIComplianceAgent.Api.Contracts;

namespace AIComplianceAgent.Api.RouteModules;

public static class HealthRouteModule
{
    public static IEndpointRouteBuilder MapHealthRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new ServiceStatusResponse("AIComplianceAgent.Api", "ok", "/swagger")))
            .WithName("GetServiceStatus")
            .WithSummary("API health check")
            .WithDescription("Returns basic API status and the Swagger UI path.");

        app.MapGet("/health", () => Results.Ok(new HealthResponse("ok")))
            .WithName("HealthCheck")
            .WithSummary("Lightweight health check");

        return app;
    }
}
