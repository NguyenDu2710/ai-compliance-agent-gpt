using AIComplianceAgent.Api.Contracts;
using AIComplianceAgent.Api.Extensions;
using AIComplianceAgent.Api.Services;
using AIComplianceAgent.Api.Validators;
using AIComplianceAgent.Core.Models;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.RouteModules;

public static class WebhookRouteModule
{
    public static IEndpointRouteBuilder MapWebhookRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/github", async (
                HttpRequest request,
                IWebhookRequestValidator validator,
                IWebhookReviewService reviewService,
                CancellationToken cancellationToken) =>
            {
                var validation = await validator.ValidateAsync(request, GitProviderKind.GitHub, cancellationToken);
                if (!validation.IsAuthorized)
                {
                    return Results.Unauthorized();
                }

                var context = await reviewService.CreateContextAsync(
                    GitProviderKind.GitHub,
                    validation.Payload,
                    validation.Headers,
                    cancellationToken);

                if (context == null)
                {
                    return Results.Ok(new WebhookIgnoredResponse(true, "unsupported_event_or_action"));
                }

                var result = await reviewService.ReviewAsync(context, cancellationToken);
                return JsonResultFactory.FromString(result);
            })
            .WithName("GitHubWebhookReview")
            .WithSummary("Handle GitHub pull request webhooks")
            .WithDescription("Validates the GitHub webhook signature, fetches changed files, runs review, and returns the review JSON.")
            .Produces<AnalysisResult>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost("/webhooks/gitlab", async (
                HttpRequest request,
                IWebhookRequestValidator validator,
                IWebhookReviewService reviewService,
                CancellationToken cancellationToken) =>
            {
                var validation = await validator.ValidateAsync(request, GitProviderKind.GitLab, cancellationToken);
                if (!validation.IsAuthorized)
                {
                    return Results.Unauthorized();
                }

                var context = await reviewService.CreateContextAsync(
                    GitProviderKind.GitLab,
                    validation.Payload,
                    validation.Headers,
                    cancellationToken);

                if (context == null)
                {
                    return Results.Ok(new WebhookIgnoredResponse(true, "unsupported_event_or_action"));
                }

                var result = await reviewService.ReviewAsync(context, cancellationToken);
                return JsonResultFactory.FromString(result);
            })
            .WithName("GitLabWebhookReview")
            .WithSummary("Handle GitLab merge request webhooks")
            .WithDescription("Validates the GitLab webhook token, fetches changed files, runs review, and returns the review JSON.")
            .Produces<AnalysisResult>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
