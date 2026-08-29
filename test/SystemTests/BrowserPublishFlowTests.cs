using System.Net;
using System.Text.Json;
using FluentAssertions;
using SystemTests.Infrastructure;
using Xunit;

namespace SystemTests;

public class BrowserPublishFlowTests : IClassFixture<SystemTestFixture>
{
    private readonly SystemTestFixture _fixture;

    public BrowserPublishFlowTests(SystemTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BrowserFlow_CreatePublishVerify_Works()
    {
        if (!_fixture.IsConfigured) return;
        var config = _fixture.Config!;
        var client = _fixture.Client!;
        var cognito = _fixture.CognitoHelper!;

        // 1. Authenticate as test user (AdminInitiateAuth simulates browser auth result)
        var token = await cognito.GetUserTokenAsync(config.User1Email, config.User1Password);
        token.Should().NotBeNullOrEmpty();

        try
        {
            // 2. Create site
            var createResp = await client.PostJsonAsync("/sites",
                new { SiteName = "E2E Browser Test" }, token);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var createJson = await createResp.Content.ReadAsStringAsync();
            using var createDoc = JsonDocument.Parse(createJson);
            var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;
            siteId.Should().NotBeNullOrEmpty();

            _fixture.TrackForCleanup(siteId, token);

            // 3. Publish ZIP
            using var zip = SystemTestClient.CreateTestZip(new Dictionary<string, string>
            {
                ["index.html"] = "<!DOCTYPE html><html><head><title>Browser Test</title></head><body><h1>Browser Test</h1></body></html>",
                ["assets/style.css"] = "body { color: blue; }",
            });

            var publishResp = await client.PutZipAsync(
                $"/sites/{siteId}/content", zip, "site.zip", token);
            publishResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var publishJson = await publishResp.Content.ReadAsStringAsync();
            using var publishDoc = JsonDocument.Parse(publishJson);
            var siteUrl = publishDoc.RootElement.GetProperty("siteUrl").GetString()!;
            siteUrl.Should().NotBeNullOrEmpty();
            siteUrl.Should().EndWith("/");

            // 4. Verify content via CloudFront URL
            var content = await FetchWithRetryAsync($"{siteUrl}index.html");
            content.Should().Contain("Browser Test");
            content.Should().Contain("<!DOCTYPE html>");

            // 5. Verify CSS asset is also reachable
            var css = await FetchWithRetryAsync($"{siteUrl}assets/style.css");
            css.Should().Contain("color: blue");
        }
        finally
        {
            // Ensure cleanup of any tracked sites
        }
    }

    private async Task<string> FetchWithRetryAsync(string url, int maxAttempts = 10)
    {
        using var http = new HttpClient();
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var response = await http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Forbidden
                    || response.StatusCode == HttpStatusCode.NotFound)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    continue;
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException) when (i < maxAttempts - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new InvalidOperationException(
            $"Failed to fetch {url} after {maxAttempts} attempts");
    }
}
