using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Api.Providers;

public class GitHubClient : IGitProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public GitHubClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
        _httpClient.BaseAddress ??= new Uri("https://api.github.com");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AIComplianceAgent/1.0");

        var token = Token;
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<DiffRequest> GetPullRequestDiffAsync(string owner, string repo, int pullNumber)
    {
        using var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/pulls/{pullNumber}/files");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var files = new List<FileDiff>();

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var filename = item.GetProperty("filename").GetString() ?? string.Empty;
            if (item.TryGetProperty("patch", out var patch) && patch.ValueKind == JsonValueKind.String)
            {
                files.Add(new FileDiff
                {
                    file_path = filename,
                    diff = patch.GetString() ?? string.Empty
                });
            }
            else
            {
                files.Add(new FileDiff
                {
                    file_path = filename,
                    diff = "+ /* binary/large file skipped */"
                });
            }
        }

        return new DiffRequest
        {
            repo = $"{owner}/{repo}",
            src_branch = $"pull/{pullNumber}/head",
            dest_branch = "pull_request_base",
            files = files
        };
    }

    public async Task PostReviewCommentAsync(string owner, string repo, int pullNumber, string markdown)
    {
        var body = JsonSerializer.Serialize(new { body = markdown });
        using var response = await _httpClient.PostAsync(
            $"/repos/{owner}/{repo}/issues/{pullNumber}/comments",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    public async Task SetStatusAsync(string owner, string repo, string sha, string state, string description)
    {
        if (string.IsNullOrWhiteSpace(sha))
        {
            return;
        }

        var githubState = state == "success" ? "success" : "failure";
        var body = JsonSerializer.Serialize(new
        {
            state = githubState,
            description,
            context = "AI Compliance Review"
        });

        using var response = await _httpClient.PostAsync(
            $"/repos/{owner}/{repo}/statuses/{sha}",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    private string Token => _config["GitHub:Token"] ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty;
}
