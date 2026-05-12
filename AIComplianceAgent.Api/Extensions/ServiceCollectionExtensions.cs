using System.Net;
using AIComplianceAgent.Api.Providers;
using AIComplianceAgent.Api.Services;
using AIComplianceAgent.Api.Tools;
using AIComplianceAgent.Api.Validators;
using AIComplianceAgent.Core.Review;
using AIComplianceAgent.Core.Tools;

namespace AIComplianceAgent.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy
                    .SetIsOriginAllowed(IsAllowedFrontendOrigin)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddHttpContextAccessor();
        services.AddHttpClient<GitHubClient>();
        services.AddHttpClient<GitLabClient>();

        services.AddScoped<IGitProviderTool, GitProviderTool>();
        services.AddScoped<IPullRequestDiffTool, PullRequestDiffTool>();
        services.AddScoped<IPolicyRuleTool, PolicyRuleTool>();
        services.AddScoped<ILlmReviewTool, LlmReviewTool>();
        services.AddScoped<IReviewMergeTool, ReviewMergeTool>();
        services.AddScoped<IReviewFormatterTool, ReviewFormatterTool>();
        services.AddScoped<IGitCommentTool, GitCommentTool>();
        services.AddScoped<IStatusCheckTool, StatusCheckTool>();
        services.AddScoped<IAuditLogTool, AuditLogTool>();
        services.AddScoped<IReviewOrchestrator, ReviewOrchestrator>();

        services.AddScoped<IReviewApiService, ReviewApiService>();
        services.AddScoped<IWebhookReviewService, WebhookReviewService>();
        services.AddScoped<IWebhookRequestValidator, WebhookRequestValidator>();

        return services;
    }

    private static bool IsAllowedFrontendOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            IPAddress.TryParse(uri.Host, out var address) && IsPrivateOrLoopback(address))
        {
            return uri.Port is 5173 or 5174 or 4173;
        }

        return false;
    }

    private static bool IsPrivateOrLoopback(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
            (bytes[0] == 10 ||
             bytes[0] == 192 && bytes[1] == 168 ||
             bytes[0] == 172 && bytes[1] is >= 16 and <= 31);
    }
}
