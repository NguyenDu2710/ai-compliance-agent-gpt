using AIComplianceAgent.Api.Contracts;
using AIComplianceAgent.Api.Security;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Validators;

public class WebhookRequestValidator : IWebhookRequestValidator
{
    private readonly IConfiguration _configuration;

    public WebhookRequestValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<WebhookValidationResult> ValidateAsync(
        HttpRequest request,
        GitProviderKind provider,
        CancellationToken cancellationToken = default)
    {
        var payload = await ReadBodyAsync(request);
        var headers = request.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var isAuthorized = provider switch
        {
            GitProviderKind.GitHub => WebhookVerifier.VerifyGitHubSignature(
                payload,
                request.Headers["X-Hub-Signature-256"].ToString(),
                GetSetting("GitHub:WebhookSecret", "GITHUB_WEBHOOK_SECRET")),
            GitProviderKind.GitLab => WebhookVerifier.VerifyGitLabToken(
                request.Headers["X-Gitlab-Token"].ToString(),
                GetSetting("GitLab:WebhookSecret", "GITLAB_WEBHOOK_SECRET")),
            _ => false
        };

        return new WebhookValidationResult(isAuthorized, payload, headers);
    }

    private string GetSetting(string key, string environmentVariable)
    {
        return _configuration[key] ?? Environment.GetEnvironmentVariable(environmentVariable) ?? string.Empty;
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        return await reader.ReadToEndAsync();
    }
}
