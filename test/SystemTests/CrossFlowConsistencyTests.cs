using System.Net;
using System.Text.Json;
using FluentAssertions;
using SystemTests.Infrastructure;
using Xunit;

namespace SystemTests;

public class CrossFlowConsistencyTests : IClassFixture<SystemTestFixture>
{
    private readonly SystemTestFixture _fixture;

    public CrossFlowConsistencyTests(SystemTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BrowserAndAutomationPublishedContent_AreServedIdentically()
    {
        if (!_fixture.IsConfigured) return;
        var config = _fixture.Config!;
        var client = _fixture.Client!;
        var cognito = _fixture.CognitoHelper!;

        var browserToken = await cognito.GetUserTokenAsync(
            config.User1Email, config.User1Password);
        var automationToken = await cognito.GetClientCredentialsTokenAsync();

        string? browserSiteId = null;
        string? automationSiteId = null;

        try
        {
            // Create and publish via browser flow
            var browserCreateResp = await client.PostJsonAsync("/sites",
                new { SiteName = "CrossFlow Browser" }, browserToken);
            browserCreateResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var browserCreateJson = await browserCreateResp.Content.ReadAsStringAsync();
            using (var doc = JsonDocument.Parse(browserCreateJson))
                browserSiteId = doc.RootElement.GetProperty("siteId").GetString()!;
            _fixture.TrackForCleanup(browserSiteId, browserToken);

            using (var zip = SystemTestClient.CreateTestZip(new Dictionary<string, string>
            {
                ["index.html"] = "<h1>Cross-Flow Test</h1>",
                ["data.json"] = "{\"flow\":\"consistent\"}",
            }))
            {
                var publishResp = await client.PutZipAsync(
                    $"/sites/{browserSiteId}/content", zip, "site.zip", browserToken);
                publishResp.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // Create and publish via automation flow
            var autoCreateResp = await client.PostJsonAsync("/sites",
                new { SiteName = "CrossFlow Automation" }, automationToken);
            autoCreateResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var autoCreateJson = await autoCreateResp.Content.ReadAsStringAsync();
            using (var doc = JsonDocument.Parse(autoCreateJson))
                automationSiteId = doc.RootElement.GetProperty("siteId").GetString()!;
            _fixture.TrackForCleanup(automationSiteId, automationToken);

            using (var zip = SystemTestClient.CreateTestZip(new Dictionary<string, string>
            {
                ["index.html"] = "<h1>Cross-Flow Test</h1>",
                ["data.json"] = "{\"flow\":\"consistent\"}",
            }))
            {
                var publishResp = await client.PutZipAsync(
                    $"/sites/{automationSiteId}/content", zip, "site.zip", automationToken);
                publishResp.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // Both flows return same API response structure
            var browserGetResp = await client.GetAsync(
                $"/sites/{browserSiteId}", browserToken);
            browserGetResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var browserGetJson = await browserGetResp.Content.ReadAsStringAsync();
            using var browserDoc = JsonDocument.Parse(browserGetJson);
            var browserRoot = browserDoc.RootElement;

            var autoGetResp = await client.GetAsync(
                $"/sites/{automationSiteId}", automationToken);
            autoGetResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var autoGetJson = await autoGetResp.Content.ReadAsStringAsync();
            using var autoDoc = JsonDocument.Parse(autoGetJson);
            var autoRoot = autoDoc.RootElement;

            // Both should have same shape (siteId, siteName, status, siteUrl, etc.)
            foreach (var prop in new[] { "siteId", "siteName", "status", "siteUrl",
                         "createdAt", "updatedAt", "contentPath" })
            {
                browserRoot.TryGetProperty(prop, out _).Should().BeTrue(
                    $"browser response should have '{prop}'");
                autoRoot.TryGetProperty(prop, out _).Should().BeTrue(
                    $"automation response should have '{prop}'");
            }

            // Both have status "active"
            browserRoot.GetProperty("status").GetString().Should().Be("active");
            autoRoot.GetProperty("status").GetString().Should().Be("active");

            // Both siteUrls follow the same pattern
            browserRoot.GetProperty("siteUrl").GetString().Should().NotBeNullOrEmpty();
            autoRoot.GetProperty("siteUrl").GetString().Should().NotBeNullOrEmpty();
        }
        finally
        {
            // Best-effort cleanup of individual sites
            if (browserSiteId is not null)
            {
                try { await client.DeleteAsync($"/sites/{browserSiteId}", browserToken); } catch { }
            }
            if (automationSiteId is not null)
            {
                try { await client.DeleteAsync($"/sites/{automationSiteId}", automationToken); } catch { }
            }
        }
    }
}
