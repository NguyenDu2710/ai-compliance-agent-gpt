using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Api.Tools;

public class AuditLogTool : IAuditLogTool
{
    private readonly ILogger<AuditLogTool> _logger;

    public AuditLogTool(ILogger<AuditLogTool> logger)
    {
        _logger = logger;
    }

    public Task WriteAsync(ReviewContext context, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AI review audit provider={Provider} repo={Repo} project={ProjectId} pr={Pr} mr={Mr} sha={Sha} message={Message}",
            context.Provider,
            string.IsNullOrWhiteSpace(context.Repo) ? "-" : $"{context.Owner}/{context.Repo}",
            string.IsNullOrWhiteSpace(context.ProjectId) ? "-" : context.ProjectId,
            context.PullRequestNumber,
            context.MergeRequestIid,
            context.HeadSha,
            message);

        return Task.CompletedTask;
    }
}
