using AIComplianceAgent.Api.Providers;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Tools;

public class StatusCheckTool : IStatusCheckTool
{
    private readonly GitHubClient _github;
    private readonly GitLabClient _gitlab;

    public StatusCheckTool(GitHubClient github, GitLabClient gitlab)
    {
        _github = github;
        _gitlab = gitlab;
    }

    public Task SetStatusAsync(ReviewContext context, ReviewResult result, CancellationToken cancellationToken = default)
    {
        var state = ResolveState(result);
        return context.Provider switch
        {
            GitProviderKind.GitHub => _github.SetStatusAsync(context.Owner, context.Repo, context.HeadSha, state, "AI Compliance review completed"),
            GitProviderKind.GitLab => _gitlab.SetStatusAsync(context.ProjectId, context.HeadSha, state, "AI Compliance review completed"),
            _ => Task.CompletedTask
        };
    }

    private static string ResolveState(ReviewResult result)
    {
        if (!string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase))
        {
            return "failure";
        }

        var failOn = Environment.GetEnvironmentVariable("REVIEW_FAIL_ON_SEVERITY") ?? "high";
        var threshold = Rank(failOn);
        return result.Issues.Any(issue => Rank(issue.Severity) >= threshold) ? "failure" : "success";
    }

    private static int Rank(string? severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };
    }
}
