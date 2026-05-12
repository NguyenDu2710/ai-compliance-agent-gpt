using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIComplianceAgent.Core.Models;

namespace AIComplianceAgent.Api.Providers;

public class GitLabClient : IGitProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public GitLabClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
        _httpClient.BaseAddress ??= new Uri(BaseUrl.TrimEnd('/'));

        var token = Token;
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<DiffRequest> GetMergeRequestDiffAsync(string projectId, int mergeRequestIid, string? token = null)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/v4/projects/{Uri.EscapeDataString(projectId)}/merge_requests/{mergeRequestIid}/changes");
        ApplyAuthorization(message, token);

        using var response = await _httpClient.SendAsync(message);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var files = new List<FileDiff>();

        if (root.TryGetProperty("changes", out var changes))
        {
            foreach (var item in changes.EnumerateArray())
            {
                files.Add(new FileDiff
                {
                    file_path = item.GetProperty("new_path").GetString() ?? string.Empty,
                    diff = item.GetProperty("diff").GetString() ?? string.Empty
                });
            }
        }

        return new DiffRequest
        {
            repo = projectId,
            src_branch = root.TryGetProperty("source_branch", out var source) ? source.GetString() ?? string.Empty : "merge_request_source",
            dest_branch = root.TryGetProperty("target_branch", out var target) ? target.GetString() ?? string.Empty : "merge_request_target",
            files = files
        };
    }

    public async Task PostReviewCommentAsync(string projectId, int mergeRequestIid, string markdown)
    {
        var body = JsonSerializer.Serialize(new { body = markdown });
        using var response = await _httpClient.PostAsync(
            $"/api/v4/projects/{Uri.EscapeDataString(projectId)}/merge_requests/{mergeRequestIid}/notes",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    public async Task SetStatusAsync(string projectId, string sha, string state, string description)
    {
        if (string.IsNullOrWhiteSpace(sha))
        {
            return;
        }

        var gitlabState = state == "success" ? "success" : "failed";
        var body = JsonSerializer.Serialize(new
        {
            state = gitlabState,
            name = "AI Compliance Review",
            description
        });

        using var response = await _httpClient.PostAsync(
            $"/api/v4/projects/{Uri.EscapeDataString(projectId)}/statuses/{sha}",
            new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    public Task<DiffRequest> GetPullRequestDiffAsync(string projectId, string mergeRequestIid, int numberOrIid)
    {
        var iid = int.TryParse(mergeRequestIid, out var parsed) ? parsed : numberOrIid;
        return GetMergeRequestDiffAsync(projectId, iid);
    }

    public Task PostReviewCommentAsync(string projectId, string mergeRequestIid, int numberOrIid, string markdown)
    {
        var iid = int.TryParse(mergeRequestIid, out var parsed) ? parsed : numberOrIid;
        return PostReviewCommentAsync(projectId, iid, markdown);
    }

    public Task SetStatusAsync(string projectId, string ignoredRepo, string sha, string state, string description)
    {
        return SetStatusAsync(projectId, sha, state, description);
    }

    private string Token => _config["GitLab:Token"] ?? Environment.GetEnvironmentVariable("GITLAB_TOKEN") ?? string.Empty;

    private string BaseUrl =>
        _config["GitLab:BaseUrl"] ??
        Environment.GetEnvironmentVariable("GITLAB_BASE_URL") ??
        "https://gitlab.com";

    private void ApplyAuthorization(HttpRequestMessage message, string? token)
    {
        var effectiveToken = string.IsNullOrWhiteSpace(token) ? Token : token.Trim();
        if (!string.IsNullOrWhiteSpace(effectiveToken))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", effectiveToken);
        }
    }
}
