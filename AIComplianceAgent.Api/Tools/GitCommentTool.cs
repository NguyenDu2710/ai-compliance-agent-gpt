using AIComplianceAgent.Api.Providers;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Tools;

public class GitCommentTool : IGitCommentTool
{
    private readonly GitHubClient _github;
    private readonly GitLabClient _gitlab;

    public GitCommentTool(GitHubClient github, GitLabClient gitlab)
    {
        _github = github;
        _gitlab = gitlab;
    }

    public Task PostSummaryCommentAsync(ReviewContext context, string markdown, CancellationToken cancellationToken = default)
    {
        return context.Provider switch
        {
            GitProviderKind.GitHub => _github.PostReviewCommentAsync(context.Owner, context.Repo, context.PullRequestNumber, markdown),
            GitProviderKind.GitLab => _gitlab.PostReviewCommentAsync(context.ProjectId, context.MergeRequestIid, markdown),
            _ => Task.CompletedTask
        };
    }

    public Task PostInlineCommentsAsync(ReviewContext context, IReadOnlyList<InlineComment> comments, CancellationToken cancellationToken = default)
    {
        // Placeholder: summary comment is supported first; provider-specific inline review APIs can plug in here.
        return Task.CompletedTask;
    }
}
