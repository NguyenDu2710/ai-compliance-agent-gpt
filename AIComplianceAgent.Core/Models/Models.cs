namespace AIComplianceAgent.Core.Models;

public class DiffRequest
{
    public string repo { get; set; } = string.Empty;
    public string src_branch { get; set; } = string.Empty;
    public string dest_branch { get; set; } = string.Empty;
    public List<FileDiff> files { get; set; } = new();
}

public class FileDiff
{
    public string file_path { get; set; } = string.Empty;
    public string diff { get; set; } = string.Empty;
}

public class Violation
{
    public string rule_id { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string file { get; set; } = string.Empty;
    public int line { get; set; }
    public string severity { get; set; } = string.Empty;
    public string suggestion { get; set; } = string.Empty;
}

public class Impact
{
    public string file { get; set; } = string.Empty;
    public int line { get; set; }
    public string issue { get; set; } = string.Empty;
    public string severity { get; set; } = string.Empty;
}

public class AnalysisResult
{
    public string status { get; set; } = string.Empty;
    public List<Violation> violations { get; set; } = new();
    public List<Impact> impacts { get; set; } = new();
    public ReviewNarrative review { get; set; } = new();
    public object? summary { get; set; }
}

public class ReviewNarrative
{
    public string verdict { get; set; } = string.Empty;
    public string overview { get; set; } = string.Empty;
    public List<string> reviewer_notes { get; set; } = new();
    public List<FileReviewSummary> changed_files { get; set; } = new();
}

public class FileReviewSummary
{
    public string file_path { get; set; } = string.Empty;
    public int added_lines { get; set; }
    public int removed_lines { get; set; }
    public bool skipped { get; set; }
    public string summary { get; set; } = string.Empty;
    public List<string> observations { get; set; } = new();
    public List<LineChange> changes { get; set; } = new();
    public List<TextReplacement> replacements { get; set; } = new();
}

public class LineChange
{
    public string type { get; set; } = string.Empty;
    public int old_line { get; set; }
    public int new_line { get; set; }
    public string content { get; set; } = string.Empty;
}

public class TextReplacement
{
    public int old_line { get; set; }
    public int new_line { get; set; }
    public string before { get; set; } = string.Empty;
    public string after { get; set; } = string.Empty;
}
