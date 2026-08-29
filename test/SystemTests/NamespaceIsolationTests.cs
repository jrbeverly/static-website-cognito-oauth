using System.Net;
using System.Text.Json;
using FluentAssertions;
using SystemTests.Infrastructure;
using Xunit;

namespace SystemTests;

public class NamespaceIsolationTests : IClassFixture<SystemTestFixture>
{
    private readonly SystemTestFixture _fixture;

    public NamespaceIsolationTests(SystemTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NamespaceIsolation_UserACannotDeleteUserBSite()
    {
        if (!_fixture.IsConfigured) return;
        var config = _fixture.Config!;
        var client = _fixture.Client!;
        var cognito = _fixture.CognitoHelper!;

        var tokenA = await cognito.GetUserTokenAsync(config.User1Email, config.User1Password);
        var tokenB = await cognito.GetUserTokenAsync(config.User2Email, config.User2Password);

        // Create site as User A
        var createResp = await client.PostJsonAsync("/sites",
            new { SiteName = "User A Isolation Site" }, tokenA);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

        _fixture.TrackForCleanup(siteId, tokenA);

        try
        {
            // User B cannot delete User A's site
            var deleteRespB = await client.DeleteAsync($"/sites/{siteId}", tokenB);
            deleteRespB.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "User B should not be able to delete User A's site");

            // User B cannot get User A's site
            var getRespB = await client.GetAsync($"/sites/{siteId}", tokenB);
            getRespB.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "User B should not be able to read User A's site");

            // User B's site list should not include User A's site
            var listRespB = await client.GetAsync("/sites", tokenB);
            listRespB.StatusCode.Should().Be(HttpStatusCode.OK);
            var listJsonB = await listRespB.Content.ReadAsStringAsync();
            using var listDocB = JsonDocument.Parse(listJsonB);
            var bSiteIds = listDocB.RootElement.EnumerateArray()
                .Select(s => s.GetProperty("siteId").GetString())
                .ToList();
            bSiteIds.Should().NotContain(siteId,
                "User B's site list should not include User A's site");

            // User A can still access their own site
            var getRespA = await client.GetAsync($"/sites/{siteId}", tokenA);
            getRespA.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            // Cleanup as User A
            try
            {
                await client.DeleteAsync($"/sites/{siteId}", tokenA);
            }
            catch { }
        }
    }

    [Fact]
    public async Task NamespaceIsolation_UserBCannotPublishToUserASite()
    {
        if (!_fixture.IsConfigured) return;
        var config = _fixture.Config!;
        var client = _fixture.Client!;
        var cognito = _fixture.CognitoHelper!;

        var tokenA = await cognito.GetUserTokenAsync(config.User1Email, config.User1Password);
        var tokenB = await cognito.GetUserTokenAsync(config.User2Email, config.User2Password);

        // Create site as User A
        var createResp = await client.PostJsonAsync("/sites",
            new { SiteName = "User A Site 2" }, tokenA);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResp.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

        _fixture.TrackForCleanup(siteId, tokenA);

        try
        {
            // User B cannot publish to User A's site
            using var zip = SystemTestClient.CreateTestZip(new Dictionary<string, string>
            {
                ["index.html"] = "<h1>User B trying to write to User A's site</h1>",
            });

            var publishRespB = await client.PutZipAsync(
                $"/sites/{siteId}/content", zip, "site.zip", tokenB);
            publishRespB.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "User B should not be able to publish to User A's site");
        }
        finally
        {
            try
            {
                await client.DeleteAsync($"/sites/{siteId}", tokenA);
            }
            catch { }
        }
    }
}
