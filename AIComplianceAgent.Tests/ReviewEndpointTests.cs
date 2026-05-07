using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIComplianceAgent.Tests;

public class ReviewEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReviewEndpointTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", string.Empty);
        _factory = factory;
    }

    [Fact]
    public async Task ReviewEndpointReturnsValidJson()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/review", new
        {
            repo = "demo",
            src_branch = "feature",
            dest_branch = "main",
            files = new[]
            {
                new
                {
                    file_path = "Demo.cs",
                    diff = "+ Console.WriteLine(\"debug\");"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("success", document.RootElement.GetProperty("status").GetString());
        Assert.True(document.RootElement.TryGetProperty("violations", out _));
    }

    [Fact]
    public async Task SwaggerDocumentIsAvailable()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("paths", out var paths));
        Assert.True(paths.TryGetProperty("/review", out _));
    }
}
