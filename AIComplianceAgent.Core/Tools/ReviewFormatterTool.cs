using System.Text;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Core.Tools;

public class ReviewFormatterTool : IReviewFormatterTool
{
    public string Format(ReviewResult result)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine("## AI Compliance Review");
        markdown.AppendLine();
        markdown.AppendLine($"Status: {result.Status}");
        markdown.AppendLine();

        if (!string.IsNullOrWhiteSpace(result.Review.overview))
        {
            markdown.AppendLine(result.Review.overview);
            markdown.AppendLine();
        }

        if (result.Status == "success" && result.Issues.Count == 0)
        {
            markdown.AppendLine("No compliance violations found.");
            markdown.AppendLine();
        }
        else
        {
            markdown.AppendLine("### Issues");
            markdown.AppendLine("| Severity | Source | Rule | File | Line | Description | Suggestion |");
            markdown.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var issue in result.Issues)
            {
                markdown.AppendLine($"| {Cell(issue.Severity)} | {Cell(issue.Source)} | {Cell(issue.RuleId)} | {Cell(issue.File)} | {issue.Line} | {Cell(issue.Description)} | {Cell(issue.Suggestion)} |");
            }

            markdown.AppendLine();
        }

        AppendChangedFiles(markdown, result);

        markdown.AppendLine();
        markdown.AppendLine("### Summary");
        markdown.AppendLine($"- Total files: {result.TotalFiles}");
        markdown.AppendLine($"- Total issues: {result.Issues.Count}");
        markdown.AppendLine($"- Inline comments: placeholder ({result.InlineComments.Count} prepared, not posted yet)");
        markdown.AppendLine($"- Truncated: {result.Truncated}");
        return markdown.ToString();
    }

    private static void AppendChangedFiles(StringBuilder markdown, ReviewResult result)
    {
        if (result.Review.changed_files.Count == 0)
        {
            return;
        }

        markdown.AppendLine("### Changed Files");
        foreach (var file in result.Review.changed_files)
        {
            markdown.AppendLine();
            markdown.AppendLine($"#### `{file.file_path}`");
            markdown.AppendLine($"- {file.summary}");

            foreach (var observation in file.observations.Take(5))
            {
                markdown.AppendLine($"- Note: {observation}");
            }

            foreach (var replacement in file.replacements.Take(6))
            {
                markdown.AppendLine($"- Line {replacement.old_line} -> {replacement.new_line}: `{Inline(replacement.before)}` => `{Inline(replacement.after)}`");
            }
        }
    }

    private static string Cell(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "-"
            : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    private static string Inline(string value)
    {
        var normalized = Cell(value).Replace("`", "'");
        return normalized.Length <= 180 ? normalized : normalized[..177] + "...";
    }
}
