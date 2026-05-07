# AI Compliance Agent

AI Compliance Agent reviews code diffs from local JSON input, a direct API call, GitHub pull request webhooks, or GitLab merge request webhooks.

## Projects

- `AIComplianceAgent.Core`: shared models, diff analyzer, agent runner, Gemini integration, and review formatter.
- `AIComplianceAgent.Cli`: local runner for `input.json`.
- `AIComplianceAgent.Api`: ASP.NET Core Web API for direct review and GitHub/GitLab webhooks.
- `AIComplianceAgent.Tests`: unit and endpoint tests.

## Local CLI

```powershell
dotnet run --project AIComplianceAgent.Cli input.json
```

If `GEMINI_API_KEY` is not set, the CLI returns the local analyzer result.

## API

```powershell
dotnet run --project AIComplianceAgent.Api
```

Swagger UI:

- `http://localhost:63152/swagger`
- `https://localhost:63151/swagger`

If you want the AI review path instead of only the local analyzer, set `GEMINI_API_KEY` before starting the API.

Manual review endpoint:

```http
POST /review
Content-Type: application/json

{
  "repo": "demo",
  "src_branch": "feature",
  "dest_branch": "main",
  "files": [
    {
      "file_path": "Demo.cs",
      "diff": "+ Console.WriteLine(\"debug\");"
    }
  ]
}
```

## Environment Variables

- `GEMINI_API_KEY`: optional Gemini API key.
- `GEMINI_MODEL`: optional model override, default `gemini-2.5-flash`.
- `GITHUB_TOKEN`: GitHub token with permission to read PR files, write PR comments, and create commit statuses.
- `GITHUB_WEBHOOK_SECRET`: GitHub webhook secret.
- `GITLAB_TOKEN`: GitLab token with permission to read MR changes, write notes, and set statuses.
- `GITLAB_WEBHOOK_SECRET`: GitLab webhook secret token.
- `GITLAB_BASE_URL`: GitLab instance URL, default `https://gitlab.com`.
- `REVIEW_FAIL_ON_SEVERITY`: `critical`, `high`, `medium`, or `low`; default `high`.
- `REVIEW_MAX_FILES`: default `50`.
- `REVIEW_MAX_ISSUES`: default `200`.

The same values can be configured in `AIComplianceAgent.Api/appsettings.json`, but secrets should be supplied via environment variables in real deployments.

Example for local development:

```powershell
$env:GEMINI_API_KEY="your-api-key"
dotnet run --project AIComplianceAgent.Api
```

## GitHub Webhook Setup

1. Deploy the API where GitHub can reach it.
2. In repository settings, add a webhook with payload URL `https://your-host/webhooks/github`.
3. Set content type to `application/json`.
4. Set the webhook secret to the same value as `GITHUB_WEBHOOK_SECRET`.
5. Subscribe to pull request events.

The API handles `pull_request` actions `opened`, `synchronize`, and `reopened`, fetches changed files with `GET /repos/{owner}/{repo}/pulls/{pull_number}/files`, runs the agent, posts a PR comment, and attempts to set a commit status.

## GitLab Webhook Setup

1. Deploy the API where GitLab can reach it.
2. In project settings, add a webhook with URL `https://your-host/webhooks/gitlab`.
3. Set the secret token to the same value as `GITLAB_WEBHOOK_SECRET`.
4. Enable merge request events.

The API handles merge request actions `open`, `update`, and `reopen`, fetches MR changes with `GET /projects/{project_id}/merge_requests/{mr_iid}/changes`, runs the agent, posts an MR note, and attempts to set a commit status.

## Docker

```powershell
docker compose up --build
```

The API listens on port `8080`.

## Validation

```powershell
dotnet build
dotnet test
```
