using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Core.Review;

public interface IGitProviderTool
{
    Task<ReviewContext?> TryCreateContextAsync(GitProviderKind provider, string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
}

public interface IPullRequestDiffTool
{
    Task<DiffRequest> GetDiffAsync(ReviewContext context, CancellationToken cancellationToken = default);
}

public interface IPolicyRuleTool
{
    Task<string> AnalyzeAsync(DiffRequest request, CancellationToken cancellationToken = default);
}

public interface ILlmReviewTool
{
    Task<string?> ReviewAsync(DiffRequest request, string policyResultJson, CancellationToken cancellationToken = default);
}

public interface IReviewMergeTool
{
    ReviewResult Merge(string policyResultJson, string? llmResultJson);
}

public interface IReviewFormatterTool
{
    string Format(ReviewResult result);
}

public interface IGitCommentTool
{
    Task PostSummaryCommentAsync(ReviewContext context, string markdown, CancellationToken cancellationToken = default);
    Task PostInlineCommentsAsync(ReviewContext context, IReadOnlyList<InlineComment> comments, CancellationToken cancellationToken = default);
}

public interface IStatusCheckTool
{
    Task SetStatusAsync(ReviewContext context, ReviewResult result, CancellationToken cancellationToken = default);
}

public interface IAuditLogTool
{
    Task WriteAsync(ReviewContext context, string message, CancellationToken cancellationToken = default);
}

public interface IReviewOrchestrator
{
    Task<ReviewResult> ReviewAsync(ReviewContext context, CancellationToken cancellationToken = default);
}
