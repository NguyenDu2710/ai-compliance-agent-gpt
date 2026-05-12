using AIComplianceAgent.Core.LLM;
using AIComplianceAgent.Core.Review;
using AIComplianceAgent.Core.Tools;

namespace AIComplianceAgent.Core.Agent;

public class AgentRunner
{
    public static async Task<string> Run(string inputJson, string? apiKeyOverride = null)
    {
        var localResult = DiffAnalyzerTool.Analyze(inputJson);

        var apiKey = string.IsNullOrWhiteSpace(apiKeyOverride)
            ? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            : apiKeyOverride.Trim();
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
        return ReviewJsonValidator.IsValid(value);
    }
}
