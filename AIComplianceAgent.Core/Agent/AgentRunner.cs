using AIComplianceAgent.Core.LLM;
using AIComplianceAgent.Core.Tools;
using System.Text.Json;

namespace AIComplianceAgent.Core.Agent;

public class AgentRunner
{
    public static async Task<string> Run(string inputJson)
    {
        var localResult = DiffAnalyzerTool.Analyze(inputJson);

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return localResult;
        }

        var prompt = ReadConfig("prompt.md");
        var instruction = ReadConfig("instruction.md");

        var gemini = new GeminiService(apiKey);
        var llmResult = await gemini.AnalyzeAsync(prompt, instruction, inputJson, localResult);
        return IsValidReviewJson(llmResult) ? llmResult! : localResult;
    }

    private static string ReadConfig(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "config", fileName);
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }

        path = Path.Combine(Directory.GetCurrentDirectory(), "config", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static bool IsValidReviewJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("status", out _) &&
                root.TryGetProperty("violations", out var violations) &&
                violations.ValueKind == JsonValueKind.Array &&
                root.TryGetProperty("impacts", out var impacts) &&
                impacts.ValueKind == JsonValueKind.Array &&
                root.TryGetProperty("review", out var review) &&
                review.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("summary", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
