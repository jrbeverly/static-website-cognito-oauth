using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using PublishingApi.Tests.Integration.Infrastructure;
using Xunit;

namespace PublishingApi.Tests.Integration.Api;

public class SiteApiTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    private const string TestSub = "user-a-sub-123";
    private const string OtherSub = "user-b-sub-456";

    public SiteApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-User", TestSub);
    }

    [Fact]
    public async Task FullSiteLifecycle_Works()
    {
        var createResponse = await _client.PostAsJsonAsync("/sites", new { SiteName = "My Test Site" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;
        siteId.Should().NotBeNullOrEmpty();

        var listResponse = await _client.GetAsync("/sites");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listJson = await listResponse.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listJson);
        var sites = listDoc.RootElement.EnumerateArray().ToList();
        sites.Should().ContainSingle(s => s.GetProperty("siteId").GetString() == siteId);

        using var zip = CreateTestZip(new Dictionary<string, string>
        {
            ["index.html"] = "<html><body>Hello</body></html>",
            ["assets/style.css"] = "body { color: red; }"
        });
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(zip);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "site.zip");

        var publishResponse = await _client.PutAsync($"/sites/{siteId}/content", content);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var objects = await _fixture.S3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "my-sites-content-test",
            Prefix = $"{TestSub}/{siteId}/"
        });
        objects.S3Objects.Should().HaveCount(2);
        objects.S3Objects.Select(o => o.Key).Should().Contain(
            $"{TestSub}/{siteId}/index.html",
            $"{TestSub}/{siteId}/assets/style.css");

        var deleteResponse = await _client.DeleteAsync($"/sites/{siteId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await _fixture.S3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "my-sites-content-test",
            Prefix = $"{TestSub}/{siteId}/"
        });
        afterDelete.S3Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task Unauthenticated_Request_Returns_401()
    {
        using var noAuthClient = _fixture.CreateClient();

        var response = await noAuthClient.GetAsync("/sites");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OwnershipIsolation_UserA_CannotAccessUserBsSite()
    {
        var createResponse = await _client.PostAsJsonAsync("/sites", new { SiteName = "User A Site" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/sites/{siteId}");
        getRequest.Headers.Add("X-Test-User", OtherSub);
        var getResponse = await _client.SendAsync(getRequest);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/sites/{siteId}");
        deleteRequest.Headers.Add("X-Test-User", OtherSub);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var getAsA = await _client.GetAsync($"/sites/{siteId}");
        getAsA.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Publish_PathTraversalInZip_Returns_400()
    {
        var createResponse = await _client.PostAsJsonAsync("/sites", new { SiteName = "Path Traversal Test" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

        using var zip = CreateTestZip(new Dictionary<string, string>
        {
            ["../evil.html"] = "<h1>Hacked</h1>"
        });
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(zip);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "site.zip");

        var publishResponse = await _client.PutAsync($"/sites/{siteId}/content", content);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await publishResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Path traversal");
    }

    [Fact]
    public async Task Publish_OversizedZip_Returns_413()
    {
        var createResponse = await _client.PostAsJsonAsync("/sites", new { SiteName = "Oversized ZIP Test" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

        var limitedClient = _fixture.CreateClient(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MAX_ZIP_SIZE_MB"] = "1"
                });
            });
        });
        limitedClient.DefaultRequestHeaders.Add("X-Test-User", TestSub);

        using var zip = CreateLargeZip(2_000_000);
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(zip);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "site.zip");

        var publishResponse = await limitedClient.PutAsync($"/sites/{siteId}/content", content);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Publish_SecondPublish_ReplacesFirst()
    {
        var createResponse = await _client.PostAsJsonAsync("/sites", new { SiteName = "Replace Test" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

        using (var zip = CreateTestZip(new Dictionary<string, string>
        {
            ["index.html"] = "<html>Version 1</html>",
            ["old.css"] = "body {}"
        }))
        {
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(zip);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", "site.zip");
            var response = await _client.PutAsync($"/sites/{siteId}/content", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var zip = CreateTestZip(new Dictionary<string, string>
        {
            ["index.html"] = "<html>Version 2</html>",
            ["new.css"] = "body { margin: 0; }"
        }))
        {
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(zip);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", "site.zip");
            var response = await _client.PutAsync($"/sites/{siteId}/content", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var objects = await _fixture.S3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "my-sites-content-test",
            Prefix = $"{TestSub}/{siteId}/"
        });

        objects.S3Objects.Should().HaveCount(2);
        var keys = objects.S3Objects.Select(o => o.Key).ToList();
        keys.Should().Contain($"{TestSub}/{siteId}/index.html");
        keys.Should().Contain($"{TestSub}/{siteId}/new.css");
        keys.Should().NotContain($"{TestSub}/{siteId}/old.css");
    }

    [Fact]
    public async Task GetSite_ReturnsCorrectSiteDetails()
    {
        var createResponse = await _client.PostAsJsonAsync("/sites", new { SiteName = "Details Test" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

        var getResponse = await _client.GetAsync($"/sites/{siteId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getJson);
        getDoc.RootElement.GetProperty("siteId").GetString().Should().Be(siteId);
        getDoc.RootElement.GetProperty("siteName").GetString().Should().Be("Details Test");
        getDoc.RootElement.GetProperty("status").GetString().Should().Be("active");
        getDoc.RootElement.GetProperty("contentPath").GetString().Should().Be($"{TestSub}/{siteId}/");
    }

    [Fact]
    public async Task GetSite_NonExistent_Returns_404()
    {
        var response = await _client.GetAsync("/sites/non-existent-id");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSite_EmptyName_Returns_400()
    {
        var response = await _client.PostAsJsonAsync("/sites", new { SiteName = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static Stream CreateTestZip(Dictionary<string, string> files)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        ms.Position = 0;
        return ms;
    }

    private static Stream CreateLargeZip(long targetSizeBytes)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("large-file.txt", CompressionLevel.NoCompression);
            using var writer = new BinaryWriter(entry.Open());
            var chunk = Encoding.UTF8.GetBytes(new string('A', 81920));
            long written = 0;
            while (written < targetSizeBytes)
            {
                var toWrite = (int)Math.Min(chunk.Length, targetSizeBytes - written);
                writer.Write(chunk, 0, toWrite);
                written += toWrite;
            }
        }
        ms.Position = 0;
        return ms;
    }
}
