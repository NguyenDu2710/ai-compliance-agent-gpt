namespace AIComplianceAgent.Api.Contracts;

public record SecretConfigurationRequest(string? GeminiApiKey, string? GitHubToken, string? GitLabToken);

public record SecretConfigurationStatus(bool HasGeminiApiKey, bool HasGitHubToken, bool HasGitLabToken);
