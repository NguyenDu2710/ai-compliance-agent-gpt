using System.Text.Json;
using AIComplianceAgent.Api.Providers;
using AIComplianceAgent.Api.Security;
using AIComplianceAgent.Api.Tools;
using AIComplianceAgent.Core.Agent;
using AIComplianceAgent.Core.Models;
using AIComplianceAgent.Core.Review;
using AIComplianceAgent.Core.Tools;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHttpClient<GitHubClient>();
builder.Services.AddHttpClient<GitLabClient>();
builder.Services.AddScoped<IGitProviderTool, GitProviderTool>();
builder.Services.AddScoped<IPullRequestDiffTool, PullRequestDiffTool>();
builder.Services.AddScoped<IPolicyRuleTool, PolicyRuleTool>();
builder.Services.AddScoped<ILlmReviewTool, LlmReviewTool>();
builder.Services.AddScoped<IReviewMergeTool, ReviewMergeTool>();
builder.Services.AddScoped<IReviewFormatterTool, ReviewFormatterTool>();
builder.Services.AddScoped<IGitCommentTool, GitCommentTool>();
builder.Services.AddScoped<IStatusCheckTool, StatusCheckTool>();
builder.Services.AddScoped<IAuditLogTool, AuditLogTool>();
builder.Services.AddScoped<IReviewOrchestrator, ReviewOrchestrator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Compliance Agent API",
        Version = "v1",
        Description = "Review code diffs directly or via GitHub/GitLab webhook payloads."
    });
});

var app = builder.Build();
ApplyAgentEnvironment(app.Configuration);
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Compliance Agent API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "AI Compliance Agent Swagger";
    options.DefaultModelsExpandDepth(-1);
});

app.MapGet("/", () => Results.Ok(new
{
    service = "AIComplianceAgent.Api",
    status = "ok",
    swagger = "/swagger"
}))
.WithName("GetServiceStatus")
.WithSummary("API health check")
.WithDescription("Returns basic API status and the Swagger UI path.");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck")
    .WithSummary("Lightweight health check");

app.MapPost("/review", async (DiffRequest request) =>
{
    var json = JsonSerializer.Serialize(request);
    var result = await AgentRunner.Run(json);
    return JsonResult(result);
})
.WithName("ReviewDiff")
.WithSummary("Run an AI compliance review for a JSON diff payload")
.WithDescription("Submit a diff payload to run local analysis and, when GEMINI_API_KEY is set, LLM-assisted review.")
.Accepts<DiffRequest>("application/json")
.Produces<AnalysisResult>(StatusCodes.Status200OK, "application/json");

app.MapPost("/webhooks/github", async (
    HttpRequest request,
    IGitProviderTool gitProviderTool,
    IReviewOrchestrator orchestrator,
    IConfiguration config) =>
{
    var payload = await ReadBodyAsync(request);
    var secret = GetSetting(config, "GitHub:WebhookSecret", "GITHUB_WEBHOOK_SECRET");
    var signature = request.Headers["X-Hub-Signature-256"].ToString();

    if (!WebhookVerifier.VerifyGitHubSignature(payload, signature, secret))
    {
        return Results.Unauthorized();
    }

    var headers = Headers(request);
    var context = await gitProviderTool.TryCreateContextAsync(GitProviderKind.GitHub, payload, headers);
    if (context == null)
    {
        return Results.Ok(new { ignored = true, reason = "unsupported_event_or_action" });
    }

    var result = await orchestrator.ReviewAsync(context);

    return JsonResult(result.RawJson);
})
.WithName("GitHubWebhookReview")
.WithSummary("Handle GitHub pull request webhooks")
.WithDescription("Validates the GitHub webhook signature, fetches changed files, runs review, and returns the review JSON.")
.Produces<AnalysisResult>(StatusCodes.Status200OK, "application/json")
.Produces(StatusCodes.Status401Unauthorized);

app.MapPost("/webhooks/gitlab", async (
    HttpRequest request,
    IGitProviderTool gitProviderTool,
    IReviewOrchestrator orchestrator,
    IConfiguration config) =>
{
    var payload = await ReadBodyAsync(request);
    var secret = GetSetting(config, "GitLab:WebhookSecret", "GITLAB_WEBHOOK_SECRET");
    var token = request.Headers["X-Gitlab-Token"].ToString();

    if (!WebhookVerifier.VerifyGitLabToken(token, secret))
    {
        return Results.Unauthorized();
    }

    var context = await gitProviderTool.TryCreateContextAsync(GitProviderKind.GitLab, payload, Headers(request));
    if (context == null)
    {
        return Results.Ok(new { ignored = true, reason = "unsupported_event_or_action" });
    }

    var result = await orchestrator.ReviewAsync(context);

    return JsonResult(result.RawJson);
})
.WithName("GitLabWebhookReview")
.WithSummary("Handle GitLab merge request webhooks")
.WithDescription("Validates the GitLab webhook token, fetches changed files, runs review, and returns the review JSON.")
.Produces<AnalysisResult>(StatusCodes.Status200OK, "application/json")
.Produces(StatusCodes.Status401Unauthorized);

app.Run();

static IResult JsonResult(string json)
{
    using var document = JsonDocument.Parse(json);
    return Results.Json(document.RootElement.Clone());
}

static async Task<string> ReadBodyAsync(HttpRequest request)
{
    using var reader = new StreamReader(request.Body);
    return await reader.ReadToEndAsync();
}

static string GetSetting(IConfiguration config, string key, string environmentVariable)
{
    return config[key] ?? Environment.GetEnvironmentVariable(environmentVariable) ?? string.Empty;
}

static IReadOnlyDictionary<string, string> Headers(HttpRequest request)
{
    return request.Headers.ToDictionary(header => header.Key, header => header.Value.ToString(), StringComparer.OrdinalIgnoreCase);
}

static void ApplyAgentEnvironment(IConfiguration config)
{
    SetEnvironmentIfMissing("GEMINI_API_KEY", config["Gemini:ApiKey"]);
    SetEnvironmentIfMissing("GEMINI_MODEL", config["Gemini:Model"]);
    SetEnvironmentIfMissing("REVIEW_FAIL_ON_SEVERITY", config["Review:FailOnSeverity"]);
    SetEnvironmentIfMissing("REVIEW_MAX_FILES", config["Review:MaxFiles"]);
    SetEnvironmentIfMissing("REVIEW_MAX_ISSUES", config["Review:MaxIssues"]);
}

static void SetEnvironmentIfMissing(string name, string? value)
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)) &&
        !string.IsNullOrWhiteSpace(value))
    {
        Environment.SetEnvironmentVariable(name, value);
    }
}

public partial class Program;
