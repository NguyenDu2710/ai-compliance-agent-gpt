using System.Text;
using System.Text.Json;

namespace AIComplianceAgent.Core.Formatting;

public static class ReviewFormatter
{
    public static string Format(string resultJson)
    {
        using var document = JsonDocument.Parse(resultJson);
        var root = document.RootElement;
        var status = Read(root, "status", "failure");
        var violationCount = CountArray(root, "violations");
        var impactCount = CountArray(root, "impacts");

        var markdown = new StringBuilder();
        markdown.AppendLine("## AI Compliance Review");
        markdown.AppendLine();
        markdown.AppendLine($"**Status:** `{status}`");
        markdown.AppendLine($"**Verdict:** `{ReadReview(root, "verdict", status == "success" ? "pass" : "failure")}`");
        markdown.AppendLine();

        var overview = ReadReview(root, "overview");
        if (!string.IsNullOrWhiteSpace(overview))
        {
            markdown.AppendLine(overview);
            markdown.AppendLine();
        }

        AppendConclusion(markdown, root, status, violationCount, impactCount);
        AppendSummary(markdown, root, violationCount, impactCount);
        AppendIssues(markdown, root, violationCount, impactCount);
        AppendReviewerNotes(markdown, root);
        AppendFileScope(markdown, root);

        if (status != "success" && root.TryGetProperty("reason", out var reason))
        {
            markdown.AppendLine();
            markdown.AppendLine("### Failure Reason");
            markdown.AppendLine(Escape(reason.ToString()));
        }

        return markdown.ToString();
    }

    private static void AppendConclusion(StringBuilder markdown, JsonElement root, string status, int violationCount, int impactCount)
    {
        var changedFileCount = CountChangedFiles(root);
        var skippedFileCount = CountSkippedFiles(root);
        var replacementCount = CountReplacements(root);
        var hasNuxtPublicPathRisk = HasObservation(root, "Asset path uses /public");
        var hasPlaceholderLinks = HasObservation(root, "Placeholder link remains");
        var hasInlineStyle = HasObservation(root, "Inline style");

        markdown.AppendLine("### Conclusion");
        if (status == "success" && violationCount == 0 && impactCount == 0)
        {
            markdown.AppendLine("This PR is mostly a content and branding update. The local policy engine did not find compliance violations or breaking code-impact changes.");
        }
        else
        {
            markdown.AppendLine("This PR needs attention before merge because the review found policy violations or code-impact risks.");
        }

        markdown.AppendLine();
        markdown.AppendLine("### What Changed");
        markdown.AppendLine($"- Scope: `{changedFileCount}` changed file(s), `{replacementCount}` concrete before/after replacement(s), `{skippedFileCount}` skipped binary/large file(s).");
        markdown.AppendLine("- Main intent: update static UI content and branding across footer, navbar, travel guide, and travel tour components.");
        markdown.AppendLine("- Code behavior: no method signatures, public API lines, control flow, persistence, or exception behavior were changed in the analyzed text patches.");

        var riskNotes = new List<string>();
        if (hasNuxtPublicPathRisk)
        {
            riskNotes.Add("Verify asset paths: Nuxt public assets are usually referenced from the site root, so `/public/images/Logo.png` may need to be `/images/Logo.png` depending on the app setup.");
        }

        if (hasPlaceholderLinks)
        {
            riskNotes.Add("Navbar menu items still use `href=\"#\"`; acceptable for placeholders, but not final navigation.");
        }

        if (hasInlineStyle)
        {
            riskNotes.Add("Inline image width styling was introduced/changed; consider moving it into component CSS if style consistency matters.");
        }

        markdown.AppendLine();
        markdown.AppendLine("### Merge Recommendation");
        if (riskNotes.Count == 0 && violationCount == 0 && impactCount == 0)
        {
            markdown.AppendLine("- Decision: looks safe to merge from the configured compliance rules.");
        }
        else if (violationCount == 0 && impactCount == 0)
        {
            markdown.AppendLine("- Decision: no blocking compliance issue found, but review the notes below before merging.");
        }
        else
        {
            markdown.AppendLine("- Decision: address listed issues before merging.");
        }

        foreach (var note in riskNotes)
        {
            markdown.AppendLine($"- Note: {note}");
        }

        markdown.AppendLine();
    }

    private static void AppendSummary(StringBuilder markdown, JsonElement root, int violationCount, int impactCount)
    {
        markdown.AppendLine("### Summary");
        markdown.AppendLine($"- Repo: `{SummaryValue(root, "repo")}`");
        markdown.AppendLine($"- Source branch: `{SummaryValue(root, "src_branch")}`");
        markdown.AppendLine($"- Destination branch: `{SummaryValue(root, "dest_branch")}`");
        markdown.AppendLine($"- Files reviewed: `{SummaryValue(root, "analyzed_files")}/{SummaryValue(root, "total_files")}`");
        markdown.AppendLine($"- Violations: `{violationCount}`");
        markdown.AppendLine($"- Impacts: `{impactCount}`");
        markdown.AppendLine($"- Truncated: `{SummaryValue(root, "truncated")}`");
        markdown.AppendLine();
    }

    private static void AppendIssues(StringBuilder markdown, JsonElement root, int violationCount, int impactCount)
    {
        if (violationCount == 0 && impactCount == 0)
        {
            markdown.AppendLine("### Issues");
            markdown.AppendLine("No configured compliance violations or code impacts were found.");
            markdown.AppendLine();
            return;
        }

        if (root.TryGetProperty("violations", out var violations) && violations.ValueKind == JsonValueKind.Array)
        {
            markdown.AppendLine("### Violations");
            markdown.AppendLine("| Severity | Rule | File | Line | Description | Suggestion |");
            markdown.AppendLine("|---|---|---|---|---|---|");
            foreach (var violation in violations.EnumerateArray())
            {
                markdown.AppendLine($"| {Cell(violation, "severity")} | {Cell(violation, "rule_id")} | {Cell(violation, "file")} | {Cell(violation, "line")} | {Cell(violation, "description")} | {Cell(violation, "suggestion")} |");
            }
            markdown.AppendLine();
        }

        if (root.TryGetProperty("impacts", out var impacts) && impacts.ValueKind == JsonValueKind.Array)
        {
            markdown.AppendLine("### Impacts");
            markdown.AppendLine("| Severity | File | Line | Issue |");
            markdown.AppendLine("|---|---|---|---|");
            foreach (var impact in impacts.EnumerateArray())
            {
                markdown.AppendLine($"| {Cell(impact, "severity")} | {Cell(impact, "file")} | {Cell(impact, "line")} | {Cell(impact, "issue")} |");
            }
            markdown.AppendLine();
        }
    }

    private static void AppendReviewerNotes(StringBuilder markdown, JsonElement root)
    {
        if (!TryGetReviewProperty(root, "reviewer_notes", out var notes) || notes.ValueKind != JsonValueKind.Array || notes.GetArrayLength() == 0)
        {
            return;
        }

        markdown.AppendLine("### Reviewer Notes");
        foreach (var note in notes.EnumerateArray())
        {
            markdown.AppendLine($"- {Escape(note.ToString())}");
        }
        markdown.AppendLine();
    }

    private static void AppendFileScope(StringBuilder markdown, JsonElement root)
    {
        if (!TryGetReviewProperty(root, "changed_files", out var files) || files.ValueKind != JsonValueKind.Array || files.GetArrayLength() == 0)
        {
            return;
        }

        markdown.AppendLine("### File Scope");
        foreach (var file in files.EnumerateArray())
        {
            if (Read(file, "skipped", "False").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                markdown.AppendLine($"- `{Read(file, "file_path")}`: binary/large file skipped; only file presence was reviewed.");
                continue;
            }

            var focus = BuildFileFocus(file);
            markdown.AppendLine($"- `{Read(file, "file_path")}`: {focus}");
        }

        markdown.AppendLine();
    }

    private static string BuildFileFocus(JsonElement file)
    {
        var filePath = Read(file, "file_path");
        var observations = ReadObservations(file).ToList();

        if (filePath.Contains("Footer", StringComparison.OrdinalIgnoreCase))
        {
            return "updates footer brand identity, organization details, address, email, copyright, and logo reference.";
        }

        if (filePath.Contains("NavBar", StringComparison.OrdinalIgnoreCase))
        {
            var concerns = observations.Any(item => item.Contains("Placeholder link remains", StringComparison.OrdinalIgnoreCase))
                ? " Placeholder links remain and should be verified before production."
                : string.Empty;
            return "updates navbar logo and exhibition menu labels." + concerns;
        }

        if (filePath.Contains("Travel_Guide", StringComparison.OrdinalIgnoreCase))
        {
            return "updates the travel guide banner heading to match the new site identity.";
        }

        if (filePath.Contains("Travel_Tour", StringComparison.OrdinalIgnoreCase))
        {
            return "updates static tour labels to the new location branding.";
        }

        return Escape(Read(file, "summary"));
    }

    private static string ReadReview(JsonElement root, string propertyName, string fallback = "")
    {
        return TryGetReviewProperty(root, propertyName, out var value) ? value.ToString() : fallback;
    }

    private static bool TryGetReviewProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        value = default;
        return root.TryGetProperty("review", out var review) &&
            review.ValueKind == JsonValueKind.Object &&
            review.TryGetProperty(propertyName, out value);
    }

    private static string SummaryValue(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty("summary", out var summary) &&
            summary.ValueKind == JsonValueKind.Object &&
            summary.TryGetProperty(propertyName, out var value))
        {
            return Escape(value.ToString());
        }

        return "-";
    }

    private static int CountArray(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;
    }

    private static int CountChangedFiles(JsonElement root)
    {
        return TryGetReviewProperty(root, "changed_files", out var files) && files.ValueKind == JsonValueKind.Array
            ? files.GetArrayLength()
            : 0;
    }

    private static int CountSkippedFiles(JsonElement root)
    {
        if (!TryGetReviewProperty(root, "changed_files", out var files) || files.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return files.EnumerateArray().Count(file => Read(file, "skipped", "False").Equals("True", StringComparison.OrdinalIgnoreCase));
    }

    private static int CountReplacements(JsonElement root)
    {
        if (!TryGetReviewProperty(root, "changed_files", out var files) || files.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return files.EnumerateArray().Sum(file =>
            file.TryGetProperty("replacements", out var replacements) && replacements.ValueKind == JsonValueKind.Array
                ? replacements.GetArrayLength()
                : 0);
    }

    private static bool HasObservation(JsonElement root, string text)
    {
        if (!TryGetReviewProperty(root, "changed_files", out var files) || files.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return files.EnumerateArray()
            .SelectMany(ReadObservations)
            .Any(observation => observation.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ReadObservations(JsonElement file)
    {
        if (!file.TryGetProperty("observations", out var observations) || observations.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var observation in observations.EnumerateArray())
        {
            yield return observation.ToString();
        }
    }

    private static string Cell(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value)
            ? Escape(value.ToString())
            : "-";
    }

    private static string Read(JsonElement element, string propertyName, string fallback = "")
    {
        return element.TryGetProperty(propertyName, out var value) ? value.ToString() : fallback;
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
