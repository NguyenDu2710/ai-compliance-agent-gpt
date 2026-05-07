using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Core.Tools;

public class ReviewOrchestrator : IReviewOrchestrator
{
    private readonly IPullRequestDiffTool _diffTool;
    private readonly IPolicyRuleTool _policyRuleTool;
    private readonly ILlmReviewTool _llmReviewTool;
    private readonly IReviewMergeTool _reviewMergeTool;
    private readonly IReviewFormatterTool _reviewFormatterTool;
    private readonly IGitCommentTool _gitCommentTool;
    private readonly IStatusCheckTool _statusCheckTool;
    private readonly IAuditLogTool _auditLogTool;

    public ReviewOrchestrator(
        IPullRequestDiffTool diffTool,
        IPolicyRuleTool policyRuleTool,
        ILlmReviewTool llmReviewTool,
        IReviewMergeTool reviewMergeTool,
        IReviewFormatterTool reviewFormatterTool,
        IGitCommentTool gitCommentTool,
        IStatusCheckTool statusCheckTool,
        IAuditLogTool auditLogTool)
    {
        _diffTool = diffTool;
        _policyRuleTool = policyRuleTool;
        _llmReviewTool = llmReviewTool;
        _reviewMergeTool = reviewMergeTool;
        _reviewFormatterTool = reviewFormatterTool;
        _gitCommentTool = gitCommentTool;
        _statusCheckTool = statusCheckTool;
        _auditLogTool = auditLogTool;
    }

    public async Task<ReviewResult> ReviewAsync(ReviewContext context, CancellationToken cancellationToken = default)
    {
        await _auditLogTool.WriteAsync(context, "review_started", cancellationToken);

        context.DiffRequest ??= await _diffTool.GetDiffAsync(context, cancellationToken);
        context.PolicyResultJson = await _policyRuleTool.AnalyzeAsync(context.DiffRequest, cancellationToken);
        context.LlmResultJson = await _llmReviewTool.ReviewAsync(context.DiffRequest, context.PolicyResultJson, cancellationToken);
        context.Result = _reviewMergeTool.Merge(context.PolicyResultJson, context.LlmResultJson);
        context.SummaryMarkdown = _reviewFormatterTool.Format(context.Result);

        await _gitCommentTool.PostSummaryCommentAsync(context, context.SummaryMarkdown, cancellationToken);
        await _gitCommentTool.PostInlineCommentsAsync(context, context.Result.InlineComments, cancellationToken);
        await _statusCheckTool.SetStatusAsync(context, context.Result, cancellationToken);
        await _auditLogTool.WriteAsync(context, $"review_finished status={context.Result.Status} issues={context.Result.Issues.Count}", cancellationToken);

        return context.Result;
    }
}
