namespace AIComplianceAgent.Api.Extensions;

public static class EnvironmentConfigurationExtensions
{
    public static void ApplyAgentEnvironment(this IConfiguration configuration)
    {
        SetEnvironmentIfMissing("GEMINI_API_KEY", configuration["Gemini:ApiKey"]);
        SetEnvironmentIfMissing("GEMINI_MODEL", configuration["Gemini:Model"]);
        SetEnvironmentIfMissing("REVIEW_FAIL_ON_SEVERITY", configuration["Review:FailOnSeverity"]);
        SetEnvironmentIfMissing("REVIEW_MAX_FILES", configuration["Review:MaxFiles"]);
        SetEnvironmentIfMissing("REVIEW_MAX_ISSUES", configuration["Review:MaxIssues"]);
    }

    private static void SetEnvironmentIfMissing(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)) &&
            !string.IsNullOrWhiteSpace(value))
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
