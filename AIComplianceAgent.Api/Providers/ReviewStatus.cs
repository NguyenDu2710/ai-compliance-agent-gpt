using System.Text.Json;

namespace AIComplianceAgent.Api.Providers;

public static class ReviewStatus
{
    public static string FromResult(string resultJson)
    {
        using var document = JsonDocument.Parse(resultJson);
        if (!document.RootElement.TryGetProperty("status", out var status) ||
            !string.Equals(status.GetString(), "success", StringComparison.OrdinalIgnoreCase))
        {
            return "failure";
        }

        return HasBlockingSeverity(document.RootElement) ? "failure" : "success";
    }

    private static bool HasBlockingSeverity(JsonElement root)
    {
        var failOn = Environment.GetEnvironmentVariable("REVIEW_FAIL_ON_SEVERITY") ?? "high";
        var threshold = Rank(failOn);
        if (!root.TryGetProperty("violations", out var violations) || violations.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var violation in violations.EnumerateArray())
        {
            if (violation.TryGetProperty("severity", out var severity) && Rank(severity.GetString()) >= threshold)
            {
                return true;
            }
        }

        return false;
    }

    private static int Rank(string? severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 3
        };
    }
}
