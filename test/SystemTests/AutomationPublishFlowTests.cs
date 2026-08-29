using System.Net;
using System.Text.Json;
using FluentAssertions;
using SystemTests.Infrastructure;
using Xunit;

namespace SystemTests;

public class AutomationPublishFlowTests : IClassFixture<SystemTestFixture>
{
    private readonly SystemTestFixture _fixture;

    public AutomationPublishFlowTests(SystemTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AutomationFlow_ClientCredentials_PublishAndVerify_Works()
    {
        if (!_fixture.IsConfigured) return;
        var config = _fixture.Config!;
        var client = _fixture.Client!;
        var cognito = _fixture.CognitoHelper!;

        // 1. Acquire token via client credentials grant
        var token = await cognito.GetClientCredentialsTokenAsync();
        token.Should().NotBeNullOrEmpty();

        try
        {
            // 2. Create site
            var createResp = await client.PostJsonAsync("/sites",
                new { SiteName = "E2E Automation Test" }, token);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);

            var createJson = await createResp.Content.ReadAsStringAsync();
            using var createDoc = JsonDocument.Parse(createJson);
            var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

            _fixture.TrackForCleanup(siteId, token);

            // 3. Publish ZIP (using PUT with raw ZIP body to match CLI/shell script flow)
            using var zip = SystemTestClient.CreateTestZip(new Dictionary<string, string>
            {
                ["index.html"] = "<!DOCTYPE html><html><head><title>Automation Test</title></head><body><h1>Automation Test</h1></body></html>",
                ["script.js"] = "console.log('Hello from automation');",
            });

            // Simulate shell script flow: raw zip upload (Content-Type: application/zip)
            var publishResp = await client.PutRawZipAsync(
                $"/sites/{siteId}/content", zip, token);
            publishResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var publishJson = await publishResp.Content.ReadAsStringAsync();
            using var publishDoc = JsonDocument.Parse(publishJson);
            var siteUrl = publishDoc.RootElement.GetProperty("siteUrl").GetString()!;
            siteUrl.Should().NotBeNullOrEmpty();

            // 4. Verify via CloudFront URL
            var content = await FetchWithRetryAsync($"{siteUrl}index.html");
            content.Should().Contain("Automation Test");

            // 5. Site appears in listing
            var listResp = await client.GetAsync("/sites", token);
            listResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var listJson = await listResp.Content.ReadAsStringAsync();
            using var listDoc = JsonDocument.Parse(listJson);
            var sites = listDoc.RootElement.EnumerateArray().ToList();
            sites.Should().Contain(s =>
                s.GetProperty("siteId").GetString() == siteId &&
                s.GetProperty("siteName").GetString() == "E2E Automation Test");
        }
        finally
        {
            // Cleanup handled by fixture
        }
    }

    private static async Task<string> FetchWithRetryAsync(string url, int maxAttempts = 10)
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
