using System.Text.Json;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Tools;

public class GitProviderTool : IGitProviderTool
{
    public Task<ReviewContext?> TryCreateContextAsync(
        GitProviderKind provider,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(payload);
        var context = provider switch
        {
            GitProviderKind.GitHub => TryCreateGitHubContext(document.RootElement, payload, headers),
            GitProviderKind.GitLab => TryCreateGitLabContext(document.RootElement, payload),
            _ => null
        };

        return Task.FromResult(context);
    }

    private static ReviewContext? TryCreateGitHubContext(JsonElement root, string payload, IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("X-GitHub-Event", out var eventName) ||
            !string.Equals(eventName, "pull_request", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var action = root.GetProperty("action").GetString();
        if (!Allowed(action, "opened", "synchronize", "reopened"))
        {
            return null;
        }

        var repository = root.GetProperty("repository");
        var pullRequest = root.GetProperty("pull_request");
        var owner = repository.GetProperty("owner").GetProperty("login").GetString() ?? string.Empty;
        var repo = repository.GetProperty("name").GetString() ?? string.Empty;

        return new ReviewContext
        {
            Provider = GitProviderKind.GitHub,
            Owner = owner,
            Repo = repo,
            PullRequestNumber = pullRequest.GetProperty("number").GetInt32(),
            SourceBranch = pullRequest.GetProperty("head").GetProperty("ref").GetString() ?? string.Empty,
            TargetBranch = pullRequest.GetProperty("base").GetProperty("ref").GetString() ?? string.Empty,
            HeadSha = pullRequest.GetProperty("head").GetProperty("sha").GetString() ?? string.Empty,
            RawPayload = payload
        };
    }

    private static ReviewContext? TryCreateGitLabContext(JsonElement root, string payload)
    {
        if (!root.TryGetProperty("object_kind", out var kind) ||
            !string.Equals(kind.GetString(), "merge_request", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var attributes = root.GetProperty("object_attributes");
        var action = attributes.TryGetProperty("action", out var actionElement) ? actionElement.GetString() : null;
        if (!Allowed(action, "open", "update", "reopen"))
        {
            return null;
        }

        var projectId = root.GetProperty("project").GetProperty("id").GetInt32().ToString();
        var sha = attributes.TryGetProperty("last_commit", out var lastCommit) &&
            lastCommit.TryGetProperty("id", out var idElement)
                ? idElement.GetString() ?? string.Empty
                : string.Empty;

        return new ReviewContext
        {
            Provider = GitProviderKind.GitLab,
            ProjectId = projectId,
            MergeRequestIid = attributes.GetProperty("iid").GetInt32(),
            SourceBranch = attributes.TryGetProperty("source_branch", out var source) ? source.GetString() ?? string.Empty : string.Empty,
            TargetBranch = attributes.TryGetProperty("target_branch", out var target) ? target.GetString() ?? string.Empty : string.Empty,
            HeadSha = sha,
            RawPayload = payload
        };
    }

    private static bool Allowed(string? action, params string[] allowed)
    {
        return action != null && allowed.Contains(action, StringComparer.OrdinalIgnoreCase);
    }
}
