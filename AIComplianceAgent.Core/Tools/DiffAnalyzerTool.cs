using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Core.Tools;

public class DiffAnalyzerTool
{
    private const int DefaultMaxFiles = 50;
    private const int DefaultMaxIssues = 200;
    private const int MaxLineChangesPerFile = 30;
    private const int MaxReplacementsPerFile = 15;

    public static string Analyze(string jsonInput)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonInput))
            {
                return Failure("INVALID_INPUT", "Input JSON is empty");
            }

            var request = JsonSerializer.Deserialize<DiffRequest>(jsonInput, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var validationError = Validate(request);
            if (validationError != null)
            {
                return Failure("INVALID_INPUT", validationError);
            }

            var maxFiles = ReadIntSetting("REVIEW_MAX_FILES", DefaultMaxFiles);
            var maxIssues = ReadIntSetting("REVIEW_MAX_ISSUES", DefaultMaxIssues);
            var result = new AnalysisResult { status = "success" };

            foreach (var file in request!.files.Take(maxFiles))
            {
                var fileReview = AnalyzeFile(file, result, maxIssues);
                result.review.changed_files.Add(fileReview);
                TrimIssues(result, maxIssues);
            }

            CompleteNarrative(request!, result);

            result.summary = new
            {
                repo = request.repo,
                src_branch = request.src_branch,
                dest_branch = request.dest_branch,
                total_files = request.files.Count,
                analyzed_files = Math.Min(request.files.Count, maxFiles),
                total_violations = result.violations.Count,
                total_impacts = result.impacts.Count,
                truncated = request.files.Count > maxFiles || IssueCount(result) >= maxIssues
            };

            return JsonSerializer.Serialize(result, JsonOptions());
        }
        catch (JsonException ex)
        {
            return Failure("INVALID_JSON", ex.Message);
        }
        catch (Exception ex)
        {
            return Failure("ANALYSIS_ERROR", ex.Message);
        }
    }

    private static FileReviewSummary AnalyzeFile(FileDiff file, AnalysisResult result, int maxIssues)
    {
        var fileReview = new FileReviewSummary
        {
            file_path = file.file_path
        };

        if (IsSkippedPatch(file.diff))
        {
            fileReview.skipped = true;
            fileReview.summary = "Patch content was not available, likely because the file is binary or too large.";
            fileReview.observations.Add("Skipped detailed line review for this file.");
            return fileReview;
        }

        var lines = file.diff.Replace("\r\n", "\n").Split('\n');
        var newLineNumber = 0;
        var oldLineNumber = 0;
        var currentMethod = string.Empty;
        var pendingRemoved = new Queue<LineChange>();

        foreach (var line in lines)
        {
            if (TryReadHunkHeader(line, out var oldStart, out var newStart))
            {
                oldLineNumber = oldStart - 1;
                newLineNumber = newStart - 1;
                currentMethod = string.Empty;
                continue;
            }

            var isAdded = line.StartsWith("+") && !line.StartsWith("+++");
            var isRemoved = line.StartsWith("-") && !line.StartsWith("---");
            var isContext = !line.StartsWith("+") && !line.StartsWith("-");

            if (isAdded || isContext)
            {
                newLineNumber++;
            }

            if (isRemoved || isContext)
            {
                oldLineNumber++;
            }

            var changedLineNumber = isRemoved ? oldLineNumber : newLineNumber;
            var code = StripDiffPrefix(line);

            if (IsMethodSignature(code))
            {
                currentMethod = code.Trim();
            }

            if (!isAdded && !isRemoved)
            {
                continue;
            }

            if (isAdded)
            {
                fileReview.added_lines++;
                var addedChange = new LineChange
                {
                    type = "added",
                    new_line = changedLineNumber,
                    content = code.Trim()
                };
                AddLineChange(fileReview, addedChange);
                PairReplacement(fileReview, pendingRemoved, addedChange);
                AddObservations(fileReview, code, addedChange, null);
            }

            if (isRemoved)
            {
                fileReview.removed_lines++;
                var removedChange = new LineChange
                {
                    type = "removed",
                    old_line = changedLineNumber,
                    content = code.Trim()
                };
                AddLineChange(fileReview, removedChange);
                pendingRemoved.Enqueue(removedChange);
            }

            if (isAdded)
            {
                AnalyzeAddedLine(file.file_path, changedLineNumber, code, result, maxIssues);
            }

            if (IsMethodSignature(code) && IssueCount(result) < maxIssues)
            {
                result.impacts.Add(new Impact
                {
                    file = file.file_path,
                    line = changedLineNumber,
                    issue = isRemoved
                        ? $"Removed method signature: {code.Trim()}"
                        : $"Added or changed method signature: {code.Trim()}",
                    severity = isRemoved ? "high" : "medium"
                });
            }

            if (isRemoved && IsPublicApi(code) && IssueCount(result) < maxIssues)
            {
                result.impacts.Add(new Impact
                {
                    file = file.file_path,
                    line = changedLineNumber,
                    issue = $"Removed public API line may break callers: {code.Trim()}",
                    severity = "high"
                });
            }

            if (isAdded && IsRiskyBehavior(code) && IssueCount(result) < maxIssues)
            {
                result.impacts.Add(new Impact
                {
                    file = file.file_path,
                    line = changedLineNumber,
                    issue = string.IsNullOrWhiteSpace(currentMethod)
                        ? "Behavioral change introduced in modified code"
                        : $"Behavioral change introduced near {currentMethod}",
                    severity = "medium"
                });
            }
        }

        CompleteFileSummary(fileReview);
        return fileReview;
    }

    private static void CompleteNarrative(DiffRequest request, AnalysisResult result)
    {
        var issueCount = IssueCount(result);
        result.review.verdict = issueCount == 0 ? "pass" : "needs_attention";
        result.review.overview = issueCount == 0
            ? $"Reviewed {Math.Min(request.files.Count, result.review.changed_files.Count)} changed file(s). No configured compliance rule was violated."
            : $"Reviewed {Math.Min(request.files.Count, result.review.changed_files.Count)} changed file(s). Found {issueCount} issue(s) that should be checked before merge.";

        if (result.review.changed_files.Any(file => file.skipped))
        {
            result.review.reviewer_notes.Add("Some files were skipped because Git provider did not include a text patch.");
        }

        if (result.review.changed_files.Any(file => file.replacements.Count > 0))
        {
            result.review.reviewer_notes.Add("The review includes concrete before/after replacements extracted from the diff.");
        }

        if (issueCount == 0)
        {
            result.review.reviewer_notes.Add("No rule violations were detected by the local policy engine.");
        }
    }

    private static void CompleteFileSummary(FileReviewSummary fileReview)
    {
        if (fileReview.skipped)
        {
            return;
        }

        if (fileReview.replacements.Count > 0)
        {
            fileReview.summary = $"Updated {fileReview.replacements.Count} before/after block(s), with {fileReview.added_lines} added line(s) and {fileReview.removed_lines} removed line(s).";
            return;
        }

        if (fileReview.added_lines > 0 || fileReview.removed_lines > 0)
        {
            fileReview.summary = $"Changed {fileReview.added_lines + fileReview.removed_lines} line(s): {fileReview.added_lines} added, {fileReview.removed_lines} removed.";
            return;
        }

        fileReview.summary = "No added or removed lines were found in the text patch.";
    }

    private static void AddLineChange(FileReviewSummary fileReview, LineChange change)
    {
        if (fileReview.changes.Count < MaxLineChangesPerFile && !string.IsNullOrWhiteSpace(change.content))
        {
            fileReview.changes.Add(change);
        }
    }

    private static void PairReplacement(FileReviewSummary fileReview, Queue<LineChange> pendingRemoved, LineChange addedChange)
    {
        if (pendingRemoved.Count == 0 || fileReview.replacements.Count >= MaxReplacementsPerFile)
        {
            return;
        }

        var removedChange = pendingRemoved.Dequeue();
        if (string.IsNullOrWhiteSpace(removedChange.content) && string.IsNullOrWhiteSpace(addedChange.content))
        {
            return;
        }

        fileReview.replacements.Add(new TextReplacement
        {
            old_line = removedChange.old_line,
            new_line = addedChange.new_line,
            before = removedChange.content,
            after = addedChange.content
        });

        AddReplacementObservations(fileReview, removedChange.content, addedChange.content);
    }

    private static void AddReplacementObservations(FileReviewSummary fileReview, string before, string after)
    {
        var beforeText = StripMarkup(before);
        var afterText = StripMarkup(after);

        if (!string.IsNullOrWhiteSpace(beforeText) &&
            !string.IsNullOrWhiteSpace(afterText) &&
            !string.Equals(beforeText, afterText, StringComparison.Ordinal))
        {
            AddObservation(fileReview, $"Text content changed: \"{TrimForDisplay(beforeText)}\" -> \"{TrimForDisplay(afterText)}\".");
        }

        if (Regex.IsMatch(before, @"https?://", RegexOptions.IgnoreCase) &&
            Regex.IsMatch(after, @"[""']/[^""']+", RegexOptions.IgnoreCase))
        {
            AddObservation(fileReview, "External asset reference was changed to a local/static asset path.");
        }

        if (!before.Contains("style=", StringComparison.OrdinalIgnoreCase) &&
            after.Contains("style=", StringComparison.OrdinalIgnoreCase))
        {
            AddObservation(fileReview, "Inline style was introduced in the changed markup.");
        }

        if (before.Contains("style=", StringComparison.OrdinalIgnoreCase) &&
            after.Contains("style=", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(before, after, StringComparison.Ordinal))
        {
            AddObservation(fileReview, "Inline style value was changed.");
        }
    }

    private static void AddObservations(FileReviewSummary fileReview, string code, LineChange addedChange, LineChange? removedChange)
    {
        if (Regex.IsMatch(code, @"href\s*=\s*[""']#[""']", RegexOptions.IgnoreCase))
        {
            AddObservation(fileReview, $"Placeholder link remains at new line {addedChange.new_line}.");
        }

        if (Regex.IsMatch(code, @"src\s*=\s*[""']/public/", RegexOptions.IgnoreCase))
        {
            AddObservation(fileReview, "Asset path uses /public; in Nuxt public assets are usually referenced from the site root.");
        }
    }

    private static void AddObservation(FileReviewSummary fileReview, string observation)
    {
        if (!fileReview.observations.Contains(observation) && fileReview.observations.Count < 12)
        {
            fileReview.observations.Add(observation);
        }
    }

    private static string StripMarkup(string value)
    {
        var withoutTags = Regex.Replace(value, "<[^>]+>", " ");
        return Regex.Replace(withoutTags, @"\s+", " ").Trim();
    }

    private static string TrimForDisplay(string value)
    {
        return value.Length <= 140 ? value : value[..137] + "...";
    }

    private static bool IsSkippedPatch(string diff)
    {
        return diff.Contains("binary/large file skipped", StringComparison.OrdinalIgnoreCase);
    }

    private static void AnalyzeAddedLine(string filePath, int lineNumber, string code, AnalysisResult result, int maxIssues)
    {
        AddViolationIf(code.Contains("Console.WriteLine"), result, maxIssues, new Violation
        {
            rule_id = "NO_DEBUG_LOG",
            description = "Debug log found in code",
            file = filePath,
            line = lineNumber,
            severity = "low",
            suggestion = "Remove Console.WriteLine or replace it with the approved logger"
        });

        AddViolationIf(Regex.IsMatch(code, @"\b(api[_-]?key|secret|password|token)\b\s*=\s*[""'][^""']+[""']", RegexOptions.IgnoreCase), result, maxIssues, new Violation
        {
            rule_id = "NO_HARDCODED_SECRET",
            description = "Potential hardcoded secret found in added code",
            file = filePath,
            line = lineNumber,
            severity = "critical",
            suggestion = "Move the value to a secure secret store or environment variable"
        });

        AddViolationIf(code.Contains("throw new Exception("), result, maxIssues, new Violation
        {
            rule_id = "NO_GENERIC_EXCEPTION",
            description = "Generic exception type found in added code",
            file = filePath,
            line = lineNumber,
            severity = "medium",
            suggestion = "Throw a specific exception type with actionable context"
        });

        AddViolationIf(Regex.IsMatch(code, @"\bThread\.Sleep\s*\("), result, maxIssues, new Violation
        {
            rule_id = "NO_THREAD_SLEEP",
            description = "Blocking sleep found in added code",
            file = filePath,
            line = lineNumber,
            severity = "medium",
            suggestion = "Use async waiting, retry policy, or a test clock instead"
        });
    }

    private static bool IsMethodSignature(string line)
    {
        var pattern = @"^\s*(public|private|protected|internal)\s+(static\s+|async\s+|virtual\s+|override\s+|sealed\s+|partial\s+)*[\w<>\[\],\?\s]+\s+\w+\s*\([^;]*\)\s*(\{|=>)?[^;]*$";
        return Regex.IsMatch(line, pattern);
    }

    private static bool IsPublicApi(string line)
    {
        return Regex.IsMatch(line, @"^\s*public\s+");
    }

    private static bool IsRiskyBehavior(string line)
    {
        return Regex.IsMatch(line, @"\b(return|throw|await|SaveChanges|Execute|Delete|Remove|Update|Insert)\b");
    }

    private static string StripDiffPrefix(string line)
    {
        return line.StartsWith("+") || line.StartsWith("-") ? line[1..] : line;
    }

    private static bool TryReadHunkHeader(string line, out int oldStart, out int newStart)
    {
        oldStart = 1;
        newStart = 1;

        var match = Regex.Match(line, @"^@@\s+-(\d+)(?:,\d+)?\s+\+(\d+)(?:,\d+)?\s+@@");
        if (!match.Success)
        {
            return false;
        }

        oldStart = int.Parse(match.Groups[1].Value);
        newStart = int.Parse(match.Groups[2].Value);
        return true;
    }

    private static string? Validate(DiffRequest? request)
    {
        if (request == null)
        {
            return "Input JSON does not match the expected diff request contract";
        }

        if (string.IsNullOrWhiteSpace(request.repo))
        {
            return "Field 'repo' is required";
        }

        if (string.IsNullOrWhiteSpace(request.src_branch))
        {
            return "Field 'src_branch' is required";
        }

        if (string.IsNullOrWhiteSpace(request.dest_branch))
        {
            return "Field 'dest_branch' is required";
        }

        if (request.files == null || request.files.Count == 0)
        {
            return "Field 'files' must contain at least one file";
        }

        var invalidFile = request.files.FirstOrDefault(file =>
            string.IsNullOrWhiteSpace(file.file_path) || file.diff == null);

        return invalidFile == null
            ? null
            : "Each file must include 'file_path' and 'diff'";
    }

    private static void AddViolationIf(bool condition, AnalysisResult result, int maxIssues, Violation violation)
    {
        if (condition && IssueCount(result) < maxIssues)
        {
            result.violations.Add(violation);
        }
    }

    private static int IssueCount(AnalysisResult result)
    {
        return result.violations.Count + result.impacts.Count;
    }

    private static void TrimIssues(AnalysisResult result, int maxIssues)
    {
        if (IssueCount(result) <= maxIssues)
        {
            return;
        }

        var remaining = maxIssues;
        if (result.violations.Count > remaining)
        {
            result.violations = result.violations.Take(remaining).ToList();
            result.impacts.Clear();
            return;
        }

        remaining -= result.violations.Count;
        result.impacts = result.impacts.Take(remaining).ToList();
    }

    private static int ReadIntSetting(string name, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0
            ? value
            : fallback;
    }

    private static string Failure(string errorCode, string reason)
    {
        return JsonSerializer.Serialize(new
        {
            status = "failure",
            error_code = errorCode,
            reason
        }, JsonOptions());
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}
