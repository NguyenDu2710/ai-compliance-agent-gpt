namespace AIComplianceAgent.Api.Contracts;

public record ReviewFromUrlRequest(string Url, string? GitHubToken = null, string? GitLabToken = null);
