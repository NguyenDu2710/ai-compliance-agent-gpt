using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Services;

public interface IWebhookReviewService
{
    Task<ReviewContext?> CreateContextAsync(
        GitProviderKind provider,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default);

    Task<string> ReviewAsync(ReviewContext context, CancellationToken cancellationToken = default);
}
