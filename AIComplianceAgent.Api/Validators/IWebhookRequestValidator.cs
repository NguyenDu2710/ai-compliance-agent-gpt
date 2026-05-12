using AIComplianceAgent.Api.Contracts;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Validators;

public interface IWebhookRequestValidator
{
    Task<WebhookValidationResult> ValidateAsync(
        HttpRequest request,
        GitProviderKind provider,
        CancellationToken cancellationToken = default);
}
