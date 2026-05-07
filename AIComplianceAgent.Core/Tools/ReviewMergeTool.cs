using System.Text.Json;
using AIComplianceAgent.Core.Models;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Core.Tools;

public class ReviewMergeTool : IReviewMergeTool
{
    public ReviewResult Merge(string policyResultJson, string? llmResultJson)
    {
        var sourceJson = string.IsNullOrWhiteSpace(llmResultJson) ? policyResultJson : llmResultJson;
        using var document = JsonDocument.Parse(sourceJson);
        var root = document.RootElement;

        var result = new ReviewResult
        {
            Status = ReadString(root, "status", "failure"),
            RawJson = sourceJson,
            TotalFiles = ReadSummaryInt(root, "total_files"),
            Truncated = ReadSummaryBool(root, "truncated")
        };

        AddNarrative(root, result);
        AddViolations(root, result);
        AddImpacts(root, result);
        AddInlinePlaceholders(result);

        return result;
    }

    private static void AddNarrative(JsonElement root, ReviewResult result)
    {
        if (!root.TryGetProperty("review", out var review) || review.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        result.Review.verdict = ReadString(review, "verdict");
        result.Review.overview = ReadString(review, "overview");

        AddStringArray(review, "reviewer_notes", result.Review.reviewer_notes);

        if (!review.TryGetProperty("changed_files", out var changedFiles) || changedFiles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var file in changedFiles.EnumerateArray())
        {
            var fileSummary = new FileReviewSummary
            {
                file_path = ReadString(file, "file_path"),
                added_lines = ReadInt(file, "added_lines"),
                removed_lines = ReadInt(file, "removed_lines"),
                skipped = ReadBool(file, "skipped"),
                summary = ReadString(file, "summary")
            };

            AddStringArray(file, "observations", fileSummary.observations);
            AddLineChanges(file, fileSummary);
            AddReplacements(file, fileSummary);
            result.Review.changed_files.Add(fileSummary);
        }
    }

    private static void AddViolations(JsonElement root, ReviewResult result)
    {
        if (!root.TryGetProperty("violations", out var violations) || violations.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var violation in violations.EnumerateArray())
        {
            result.Issues.Add(new ReviewIssue
            {
                Source = "policy",
                RuleId = ReadString(violation, "rule_id"),
                Severity = ReadString(violation, "severity"),
                File = ReadString(violation, "file"),
                Line = ReadInt(violation, "line"),
                Description = ReadString(violation, "description"),
                Suggestion = ReadString(violation, "suggestion")
            });
        }
    }

    private static void AddImpacts(JsonElement root, ReviewResult result)
    {
        if (!root.TryGetProperty("impacts", out var impacts) || impacts.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var impact in impacts.EnumerateArray())
        {
            result.Issues.Add(new ReviewIssue
            {
                Source = "impact",
                RuleId = "IMPACT_ANALYSIS",
                Severity = ReadString(impact, "severity"),
                File = ReadString(impact, "file"),
                Line = ReadInt(impact, "line"),
                Description = ReadString(impact, "issue")
            });
        }
    }

    private static void AddInlinePlaceholders(ReviewResult result)
    {
        foreach (var issue in result.Issues.Where(issue => !string.IsNullOrWhiteSpace(issue.File) && issue.Line > 0))
        {
            result.InlineComments.Add(new InlineComment
            {
                File = issue.File,
                Line = issue.Line,
                Body = $"[{issue.Severity}] {issue.Description}"
            });
        }
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback = "")
    {
        return element.TryGetProperty(propertyName, out var value) ? value.ToString() : fallback;
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number) ? number : 0;
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;
    }

    private static void AddStringArray(JsonElement element, string propertyName, List<string> target)
    {
        if (!element.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in array.EnumerateArray())
        {
            target.Add(item.ToString());
        }
    }

    private static void AddLineChanges(JsonElement file, FileReviewSummary fileSummary)
    {
        if (!file.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var change in changes.EnumerateArray())
        {
            fileSummary.changes.Add(new LineChange
            {
                type = ReadString(change, "type"),
                old_line = ReadInt(change, "old_line"),
                new_line = ReadInt(change, "new_line"),
                content = ReadString(change, "content")
            });
        }
    }

    private static void AddReplacements(JsonElement file, FileReviewSummary fileSummary)
    {
        if (!file.TryGetProperty("replacements", out var replacements) || replacements.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var replacement in replacements.EnumerateArray())
        {
            fileSummary.replacements.Add(new TextReplacement
            {
                old_line = ReadInt(replacement, "old_line"),
                new_line = ReadInt(replacement, "new_line"),
                before = ReadString(replacement, "before"),
                after = ReadString(replacement, "after")
            });
        }
    }

    private static int ReadSummaryInt(JsonElement root, string propertyName)
    {
        return root.TryGetProperty("summary", out var summary) ? ReadInt(summary, propertyName) : 0;
    }

    private static bool ReadSummaryBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty("summary", out var summary) &&
            summary.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.True;
    }
}
