using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Services;

public class WebhookReviewService : IWebhookReviewService
{
    private readonly IGitProviderTool _gitProviderTool;
    private readonly IReviewOrchestrator _reviewOrchestrator;

    public WebhookReviewService(IGitProviderTool gitProviderTool, IReviewOrchestrator reviewOrchestrator)
    {
        _gitProviderTool = gitProviderTool;
        _reviewOrchestrator = reviewOrchestrator;
    }

    public Task<ReviewContext?> CreateContextAsync(
        GitProviderKind provider,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        return _gitProviderTool.TryCreateContextAsync(provider, payload, headers, cancellationToken);
    }

    public async Task<string> ReviewAsync(ReviewContext context, CancellationToken cancellationToken = default)
    {
        var result = await _reviewOrchestrator.ReviewAsync(context, cancellationToken);
        return result.RawJson;
    }
}
