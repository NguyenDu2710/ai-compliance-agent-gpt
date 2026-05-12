using System.Text.Json;
using AIComplianceAgent.Api.Contracts;
using AIComplianceAgent.Api.Extensions;
using AIComplianceAgent.Api.Providers;
using AIComplianceAgent.Api.Security;
using AIComplianceAgent.Api.Services;
using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Api.RouteModules;

public static class ReviewRouteModule
{
    public static IEndpointRouteBuilder MapReviewRoutes(this IEndpointRouteBuilder app)
    {
        static async Task<IResult> RunReview(DiffRequest request, IReviewApiService reviewService, CancellationToken cancellationToken)
        {
            var result = await reviewService.ReviewAsync(request, cancellationToken);
            return JsonResultFactory.FromString(result);
        }

        app.MapPost("/review", RunReview)
            .WithName("ReviewDiff")
            .WithSummary("Run an AI compliance review for a JSON diff payload")
            .WithDescription("Submit a diff payload to run local analysis and, when GEMINI_API_KEY is set, LLM-assisted review.")
            .Accepts<DiffRequest>("application/json")
            .Produces<AnalysisResult>(StatusCodes.Status200OK, "application/json");

        app.MapPost("/api/review", RunReview)
            .WithName("ReviewDiffApi")
            .WithSummary("Run an AI compliance review for the frontend")
            .WithDescription("Frontend-friendly alias for POST /review.")
            .Accepts<DiffRequest>("application/json")
            .Produces<AnalysisResult>(StatusCodes.Status200OK, "application/json");

        app.MapPost("/api/review/test", async (IReviewApiService reviewService, CancellationToken cancellationToken) =>
            {
                var request = new DiffRequest
                {
                    repo = "demo/repository",
                    src_branch = "feature/sample-review",
                    dest_branch = "main",
                    files =
                    [
                        new FileDiff
                        {
                            file_path = "Demo.cs",
                            diff = "@@ -1,3 +1,4 @@\n public void Run()\n {\n+    Console.WriteLine(\"debug\");\n }\n"
                        }
                    ]
                };

                var result = await reviewService.ReviewAsync(request, cancellationToken);
                return JsonResultFactory.FromString(result);
            })
            .WithName("ReviewDiffTest")
            .WithSummary("Run a sample review for API connection testing")
            .Produces<AnalysisResult>(StatusCodes.Status200OK, "application/json");

        app.MapPost("/api/review/from-url", async (
                ReviewFromUrlRequest request,
                GitHubClient github,
                GitLabClient gitlab,
                IReviewApiService reviewService,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Url) ||
                    !Uri.TryCreate(request.Url.Trim(), UriKind.Absolute, out var uri))
                {
                    return Results.BadRequest(new { error = "A valid GitHub pull request or GitLab merge request URL is required." });
                }

                DiffRequest diffRequest;
                if (TryParseGitHubPullRequest(uri, out var owner, out var repo, out var pullNumber))
                {
                    var token = GetSecretCookie(httpContext, SecretCookieNames.GitHubToken, request.GitHubToken);
                    diffRequest = await github.GetPullRequestDiffAsync(owner, repo, pullNumber, token);
                }
                else if (TryParseGitLabMergeRequest(uri, out var projectPath, out var mergeRequestIid))
                {
                    var token = GetSecretCookie(httpContext, SecretCookieNames.GitLabToken, request.GitLabToken);
                    diffRequest = await gitlab.GetMergeRequestDiffAsync(projectPath, mergeRequestIid, token);
                    diffRequest.repo = projectPath;
                }
                else
                {
                    return Results.BadRequest(new { error = "Supported links: https://github.com/{owner}/{repo}/pull/{number} or https://gitlab.com/{group}/{project}/-/merge_requests/{iid}." });
                }

                var resultJson = await reviewService.ReviewAsync(diffRequest, cancellationToken);
                using var document = JsonDocument.Parse(resultJson);
                return Results.Json(new
                {
                    request = diffRequest,
                    result = document.RootElement.Clone()
                });
            })
            .WithName("ReviewFromUrl")
            .WithSummary("Fetch a GitHub PR or GitLab MR diff and run review")
            .Accepts<ReviewFromUrlRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

    public static IEndpointRouteBuilder MapSecretConfigurationRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config/secrets", (HttpContext httpContext) =>
            Results.Ok(new SecretConfigurationStatus(
                httpContext.Request.Cookies.ContainsKey(SecretCookieNames.GeminiApiKey),
                httpContext.Request.Cookies.ContainsKey(SecretCookieNames.GitHubToken),
                httpContext.Request.Cookies.ContainsKey(SecretCookieNames.GitLabToken))))
            .WithName("GetSecretConfigurationStatus")
            .WithSummary("Return which local secret cookies are configured");

        app.MapPost("/api/config/secrets", (SecretConfigurationRequest request, HttpContext httpContext) =>
            {
                SetOrDeleteSecretCookie(httpContext, SecretCookieNames.GeminiApiKey, request.GeminiApiKey);
                SetOrDeleteSecretCookie(httpContext, SecretCookieNames.GitHubToken, request.GitHubToken);
                SetOrDeleteSecretCookie(httpContext, SecretCookieNames.GitLabToken, request.GitLabToken);

                return Results.Ok(new SecretConfigurationStatus(
                    !string.IsNullOrWhiteSpace(request.GeminiApiKey),
                    !string.IsNullOrWhiteSpace(request.GitHubToken),
                    !string.IsNullOrWhiteSpace(request.GitLabToken)));
            })
            .WithName("SaveSecretConfiguration")
            .WithSummary("Store local API keys in HttpOnly cookies")
            .Accepts<SecretConfigurationRequest>("application/json");

        app.MapDelete("/api/config/secrets", (HttpContext httpContext) =>
            {
                DeleteSecretCookie(httpContext, SecretCookieNames.GeminiApiKey);
                DeleteSecretCookie(httpContext, SecretCookieNames.GitHubToken);
                DeleteSecretCookie(httpContext, SecretCookieNames.GitLabToken);

                return Results.Ok(new SecretConfigurationStatus(false, false, false));
            })
            .WithName("ClearSecretConfiguration")
            .WithSummary("Remove local API key cookies");

        return app;
    }

    private static bool TryParseGitHubPullRequest(Uri uri, out string owner, out string repo, out int pullNumber)
    {
        owner = string.Empty;
        repo = string.Empty;
        pullNumber = 0;

        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 4 || !segments[2].Equals("pull", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        owner = segments[0];
        repo = segments[1];
        return int.TryParse(segments[3], out pullNumber);
    }

    private static bool TryParseGitLabMergeRequest(Uri uri, out string projectPath, out int mergeRequestIid)
    {
        projectPath = string.Empty;
        mergeRequestIid = 0;

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var markerIndex = Array.FindIndex(segments, value => value == "-");
        if (markerIndex <= 0 ||
            segments.Length <= markerIndex + 2 ||
            !segments[markerIndex + 1].Equals("merge_requests", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        projectPath = string.Join('/', segments.Take(markerIndex));
        return int.TryParse(segments[markerIndex + 2], out mergeRequestIid);
    }

    private static string? GetSecretCookie(HttpContext httpContext, string cookieName, string? fallback)
    {
        return httpContext.Request.Cookies[cookieName] ?? fallback;
    }

    private static void SetOrDeleteSecretCookie(HttpContext httpContext, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            DeleteSecretCookie(httpContext, name);
            return;
        }

        httpContext.Response.Cookies.Append(name, value.Trim(), CreateSecretCookieOptions(httpContext));
    }

    private static void DeleteSecretCookie(HttpContext httpContext, string name)
    {
        httpContext.Response.Cookies.Delete(name, CreateSecretCookieOptions(httpContext));
    }

    private static CookieOptions CreateSecretCookieOptions(HttpContext httpContext)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/"
        };
    }
}
