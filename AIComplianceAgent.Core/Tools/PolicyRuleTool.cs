using System.Text.Json;
using AIComplianceAgent.Core.Models;
using AIComplianceAgent.Core.Review;

namespace AIComplianceAgent.Core.Tools;

public class PolicyRuleTool : IPolicyRuleTool
{
    public Task<string> AnalyzeAsync(DiffRequest request, CancellationToken cancellationToken = default)
    {
        var inputJson = JsonSerializer.Serialize(request);
        return Task.FromResult(DiffAnalyzerTool.Analyze(inputJson));
    }
}
