using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Api.Services;

public interface IReviewApiService
{
    Task<string> ReviewAsync(DiffRequest request, CancellationToken cancellationToken = default);
}
