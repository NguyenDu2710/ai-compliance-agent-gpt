using AIComplianceAgent.Core.Review;
using Xunit;

namespace AIComplianceAgent.Tests;

public class ReviewJsonValidatorTests
{
    [Fact]
    public void AcceptsLegacyReviewSchema()
    {
        const string json = """
        {
          "status": "success",
          "violations": [],
          "impacts": [],
          "review": {
            "verdict": "pass",
            "overview": "Looks safe.",
            "reviewer_notes": [],
            "changed_files": []
          },
          "summary": {
            "total_files": 1
          }
        }
        """;

        Assert.True(ReviewJsonValidator.IsValid(json));
    }

    [Fact]
    public void AcceptsPromptDrivenReviewSchema()
    {
        const string json = """
        {
          "status": "success",
          "review": {
            "verdict": "needs_attention",
            "overview": "Needs a quick manual check.",
            "reviewer_notes": []
          },
          "issues": [],
          "files": [],
          "summary": {
            "total_files": 1,
            "high_risk_files": 0,
            "requires_manual_testing": false
          }
        }
        """;

        Assert.True(ReviewJsonValidator.IsValid(json));
    }
}
