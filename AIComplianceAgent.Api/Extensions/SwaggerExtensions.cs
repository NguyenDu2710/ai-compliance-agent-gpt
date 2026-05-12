using Microsoft.OpenApi;

namespace AIComplianceAgent.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AI Compliance Agent API",
                Version = "v1",
                Description = "Review code diffs directly or via GitHub/GitLab webhook payloads."
            });
        });

        return services;
    }

    public static WebApplication UseApiSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Compliance Agent API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "AI Compliance Agent Swagger";
            options.DefaultModelsExpandDepth(-1);
        });

        return app;
    }
}
