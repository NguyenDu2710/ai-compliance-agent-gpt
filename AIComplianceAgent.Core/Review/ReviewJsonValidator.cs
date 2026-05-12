using System.Text.Json;

namespace AIComplianceAgent.Core.Review;

public static class ReviewJsonValidator
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryGetProperty(root, "status", out var status) || status.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            if (!TryGetProperty(root, "review", out var review) || review.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryGetProperty(root, "summary", out var summary) || summary.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var hasLegacyCollections =
                TryGetProperty(root, "violations", out var violations) && violations.ValueKind == JsonValueKind.Array &&
                TryGetProperty(root, "impacts", out var impacts) && impacts.ValueKind == JsonValueKind.Array;

            var hasPromptCollections =
                TryGetProperty(root, "issues", out var issues) && issues.ValueKind == JsonValueKind.Array &&
                TryGetProperty(root, "files", out var files) && files.ValueKind == JsonValueKind.Array;

            return hasLegacyCollections || hasPromptCollections;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
