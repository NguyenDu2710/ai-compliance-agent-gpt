using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Api.Providers;

public interface IGitProviderClient
{
    Task<DiffRequest> GetPullRequestDiffAsync(string ownerOrProjectId, string repoOrMergeRequestIid, int numberOrIid);
    Task PostReviewCommentAsync(string ownerOrProjectId, string repoOrMergeRequestIid, int numberOrIid, string markdown);
    Task SetStatusAsync(string ownerOrProjectId, string repoOrSha, string shaOrStatus, string state, string description);
}
