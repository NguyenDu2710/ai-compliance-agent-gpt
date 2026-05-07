using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Core.Review;

public enum GitProviderKind
{
    GitHub,
    GitLab
}

public class ReviewContext
{
    public GitProviderKind Provider { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Repo { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public int PullRequestNumber { get; set; }
    public int MergeRequestIid { get; set; }
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string HeadSha { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public DiffRequest? DiffRequest { get; set; }
    public string? PolicyResultJson { get; set; }
    public string? LlmResultJson { get; set; }
    public ReviewResult? Result { get; set; }
    public string SummaryMarkdown { get; set; } = string.Empty;
}

public class ReviewIssue
{
    public string Source { get; set; } = "policy";
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}

public class InlineComment
{
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Body { get; set; } = string.Empty;
    public string DiffSide { get; set; } = "RIGHT";
}

public class ReviewResult
{
    public string Status { get; set; } = "success";
    public ReviewNarrative Review { get; set; } = new();
    public List<ReviewIssue> Issues { get; set; } = new();
    public List<InlineComment> InlineComments { get; set; } = new();
    public int TotalFiles { get; set; }
    public bool Truncated { get; set; }
    public string RawJson { get; set; } = string.Empty;
}
