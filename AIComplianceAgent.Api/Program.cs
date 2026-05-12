using AIComplianceAgent.Api.Extensions;
using AIComplianceAgent.Api.RouteModules;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddApiServices();
builder.Services.AddApiSwagger();

var app = builder.Build();

app.Configuration.ApplyAgentEnvironment();
app.UseApiSwagger();
app.UseCors("Frontend");
app.MapHealthRoutes();
app.MapReviewRoutes();
app.MapSecretConfigurationRoutes();
app.MapWebhookRoutes();

app.Run();

public partial class Program;
