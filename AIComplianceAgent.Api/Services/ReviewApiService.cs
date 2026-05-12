using System.Text.Json;
using AIComplianceAgent.Api.Security;
using AIComplianceAgent.Core.Agent;
using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Api.Services;

public class ReviewApiService : IReviewApiService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReviewApiService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string> ReviewAsync(DiffRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request);
        var apiKey = _httpContextAccessor.HttpContext?.Request.Cookies[SecretCookieNames.GeminiApiKey];
        return AgentRunner.Run(json, apiKey);
    }
}
