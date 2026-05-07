using System.Security.Cryptography;
using System.Text;
using AIComplianceAgent.Api.Security;
using Xunit;

namespace AIComplianceAgent.Tests;

public class WebhookVerifierTests
{
    [Fact]
    public void GitHubSignatureVerifierAcceptsValidSignature()
    {
        var payload = "{\"action\":\"opened\"}";
        var secret = "test-secret";
        var signature = "sha256=" + Sign(payload, secret);

        Assert.True(WebhookVerifier.VerifyGitHubSignature(payload, signature, secret));
    }

    [Fact]
    public void GitHubSignatureVerifierRejectsInvalidSignature()
    {
        Assert.False(WebhookVerifier.VerifyGitHubSignature("{\"action\":\"opened\"}", "sha256=bad", "test-secret"));
    }

    [Fact]
    public void GitLabTokenVerifierAcceptsValidToken()
    {
        Assert.True(WebhookVerifier.VerifyGitLabToken("test-secret", "test-secret"));
    }

    [Fact]
    public void GitLabTokenVerifierRejectsInvalidToken()
    {
        Assert.False(WebhookVerifier.VerifyGitLabToken("bad", "test-secret"));
    }

    private static string Sign(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
