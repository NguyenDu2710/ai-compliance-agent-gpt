using System.Text.Json;
using AIComplianceAgent.Core.Tools;
using Xunit;

namespace AIComplianceAgent.Tests;

public class DiffAnalyzerToolTests
{
    [Fact]
    public void AnalyzeDetectsConsoleWriteLine()
    {
        var result = Analyze("+ Console.WriteLine(\"debug\");");

        Assert.Contains(result.RootElement.GetProperty("violations").EnumerateArray(),
            violation => violation.GetProperty("rule_id").GetString() == "NO_DEBUG_LOG");
    }

    [Fact]
    public void AnalyzeDetectsHardcodedSecret()
    {
        var result = Analyze("+ var api_key = \"abc123\";");

        Assert.Contains(result.RootElement.GetProperty("violations").EnumerateArray(),
            violation => violation.GetProperty("rule_id").GetString() == "NO_HARDCODED_SECRET");
    }

    [Fact]
    public void AnalyzeParsesLineNumberFromHunkHeader()
    {
        var result = Analyze("@@ -4,0 +20,2 @@\n+ Console.WriteLine(\"debug\");");

        var violation = result.RootElement.GetProperty("violations").EnumerateArray().Single();
        Assert.Equal(20, violation.GetProperty("line").GetInt32());
    }

    [Fact]
    public void AnalyzeIncludesHumanReadableReviewNarrative()
    {
        var result = Analyze("@@ -1,2 +1,2 @@\n- <h1>Tay Ho 360</h1>\n+ <h1>Dai Mo 360</h1>");

        var review = result.RootElement.GetProperty("review");
        Assert.Equal("pass", review.GetProperty("verdict").GetString());
        var changedFile = review.GetProperty("changed_files").EnumerateArray().Single();
        Assert.Equal("Demo.cs", changedFile.GetProperty("file_path").GetString());
        Assert.Equal(1, changedFile.GetProperty("replacements").GetArrayLength());
        Assert.Contains("Text content changed", changedFile.GetProperty("observations").EnumerateArray().First().GetString());
    }

    private static JsonDocument Analyze(string diff)
    {
        var json = JsonSerializer.Serialize(new
        {
            repo = "demo",
            src_branch = "feature",
            dest_branch = "main",
            files = new[]
            {
                new
                {
                    file_path = "Demo.cs",
                    diff
                }
            }
        });

        return JsonDocument.Parse(DiffAnalyzerTool.Analyze(json));
    }
}
