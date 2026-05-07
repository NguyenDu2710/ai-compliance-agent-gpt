using AIComplianceAgent.Api.Providers;
using AIComplianceAgent.Core.Models;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Tools;

public class PullRequestDiffTool : IPullRequestDiffTool
{
    private readonly GitHubClient _github;
    private readonly GitLabClient _gitlab;

    public PullRequestDiffTool(GitHubClient github, GitLabClient gitlab)
    {
        _github = github;
        _gitlab = gitlab;
    }

    public async Task<DiffRequest> GetDiffAsync(ReviewContext context, CancellationToken cancellationToken = default)
    {
        return context.Provider switch
        {
            GitProviderKind.GitHub => await GetGitHubDiffAsync(context),
            GitProviderKind.GitLab => await GetGitLabDiffAsync(context),
            _ => throw new InvalidOperationException($"Unsupported provider: {context.Provider}")
        };
    }

    private async Task<DiffRequest> GetGitHubDiffAsync(ReviewContext context)
    {
        var request = await _github.GetPullRequestDiffAsync(context.Owner, context.Repo, context.PullRequestNumber);
        request.src_branch = context.SourceBranch;
        request.dest_branch = context.TargetBranch;
        return request;
    }

    private async Task<DiffRequest> GetGitLabDiffAsync(ReviewContext context)
    {
        var request = await _gitlab.GetMergeRequestDiffAsync(context.ProjectId, context.MergeRequestIid);
        request.src_branch = string.IsNullOrWhiteSpace(request.src_branch) ? context.SourceBranch : request.src_branch;
        request.dest_branch = string.IsNullOrWhiteSpace(request.dest_branch) ? context.TargetBranch : request.dest_branch;
        return request;
    }
}
