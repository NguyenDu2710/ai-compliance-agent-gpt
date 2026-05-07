using System.Security.Cryptography;
using System.Text;

namespace AIComplianceAgent.Api.Security;

public static class WebhookVerifier
{
    public static bool VerifyGitHubSignature(string payload, string signatureHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        const string prefix = "sha256=";
        if (!signatureHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = prefix + Convert.ToHexString(hash).ToLowerInvariant();
        return FixedTimeEquals(expected, signatureHeader.ToLowerInvariant());
    }

    public static bool VerifyGitLabToken(string tokenHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(tokenHeader))
        {
            return false;
        }

        return FixedTimeEquals(secret, tokenHeader);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
            CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
