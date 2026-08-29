using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using PublishingApi.Tests.Integration.Infrastructure;
using Xunit;

namespace PublishingApi.Tests.Integration.Api;

public class ServingPathTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;
    private readonly AmazonS3Client _s3;

    private const string TestSub = "test-user-abc123";
    private const string OtherSub = "other-user";

    public ServingPathTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-User", TestSub);
        _s3 = fixture.S3;
    }

    [Fact]
    public async Task ContentStoredAtCorrectS3Keys_CanBeRetrievedFromS3()
    {
        var siteId = await CreateAndPublishSite("canonical-keys", new Dictionary<string, string>
        {
            ["index.html"] = "<!DOCTYPE html><html><body>Hello</body></html>",
            ["assets/style.css"] = "body { font-family: sans-serif; }"
        });

        var indexKey = $"{TestSub}/{siteId}/index.html";
        var cssKey = $"{TestSub}/{siteId}/assets/style.css";

        var indexResponse = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "my-sites-content-test",
            Key = indexKey
        });
        using var indexReader = new StreamReader(indexResponse.ResponseStream);
        var indexBody = await indexReader.ReadToEndAsync();
        indexBody.Should().Contain("<body>Hello</body>");

        var cssResponse = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "my-sites-content-test",
            Key = cssKey
        });
        using var cssReader = new StreamReader(cssResponse.ResponseStream);
        var cssBody = await cssReader.ReadToEndAsync();
        cssBody.Should().Contain("font-family: sans-serif");
    }

    [Fact]
    public async Task NonExistentS3Key_ReturnsNoSuchKey()
    {
        var act = () => _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "my-sites-content-test",
            Key = $"{TestSub}/non-existent-site/index.html"
        });

        await act.Should().ThrowAsync<AmazonS3Exception>()
            .Where(ex => ex.StatusCode == HttpStatusCode.NotFound
                         || ex.ErrorCode == "NoSuchKey");
    }

    [Fact]
    public async Task ContentIsNamespaceIsolatedBySub_InS3()
    {
        var siteId = await CreateAndPublishSite("isolated-site", new Dictionary<string, string>
        {
            ["index.html"] = "<html><body>User A Content</body></html>"
        });

        var userAKey = $"{TestSub}/{siteId}/index.html";

        var aResponse = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "my-sites-content-test",
            Key = userAKey
        });
        aResponse.Should().NotBeNull();

        var act = () => _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "my-sites-content-test",
            Key = $"{OtherSub}/{siteId}/index.html"
        });
        await act.Should().ThrowAsync<AmazonS3Exception>()
            .Where(ex => ex.StatusCode == HttpStatusCode.NotFound
                         || ex.ErrorCode == "NoSuchKey");
    }

    [Fact]
    public async Task MultipleSites_StoredUnderCorrectUserPrefix()
    {
        var site1Id = await CreateAndPublishSite("site-one", new Dictionary<string, string>
        {
            ["index.html"] = "Site 1"
        });
        var site2Id = await CreateAndPublishSite("site-two", new Dictionary<string, string>
        {
            ["index.html"] = "Site 2"
        });

        var site1Key = $"{TestSub}/{site1Id}/index.html";
        var site2Key = $"{TestSub}/{site2Id}/index.html";

        using var r1 = (await _s3.GetObjectAsync(new GetObjectRequest
            { BucketName = "my-sites-content-test", Key = site1Key })).ResponseStream;
        using var r2 = (await _s3.GetObjectAsync(new GetObjectRequest
            { BucketName = "my-sites-content-test", Key = site2Key })).ResponseStream;

        (await new StreamReader(r1).ReadToEndAsync()).Should().Be("Site 1");
        (await new StreamReader(r2).ReadToEndAsync()).Should().Be("Site 2");
    }

    [Fact]
    public async Task ContentRoundTrip_PreservesEncoding()
    {
        var html = "<!DOCTYPE html>\n<html lang=\"en\">\n<head><meta charset=\"utf-8\">" +
                   "</head>\n<body><h1>Round Trip</h1></body>\n</html>\n";

        var siteId = await CreateAndPublishSite("Round Trip Site", new Dictionary<string, string>
        {
            ["index.html"] = html
        });

        var s3Response = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "my-sites-content-test",
            Key = $"{TestSub}/{siteId}/index.html"
        });
        using var reader = new StreamReader(s3Response.ResponseStream);
        var roundTripped = await reader.ReadToEndAsync();

        roundTripped.Should().Be(html);
    }

    [Fact]
    public async Task DirectoryIndexPath_ResolvedByS3KeyStructure()
    {
        // The CloudFront Function rewrites /{sub}/{siteId}/ to /{sub}/{siteId}/index.html
        // This test validates the S3 key structure that the rewrite depends on
        var siteId = await CreateAndPublishSite("dir-site", new Dictionary<string, string>
        {
            ["index.html"] = "<html><body>Directory Index</body></html>"
        });

        var resolvedKey = $"{TestSub}/{siteId}/index.html";

        var s3Response = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "my-sites-content-test",
            Key = resolvedKey
        });
        using var reader = new StreamReader(s3Response.ResponseStream);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("Directory Index");
    }

    private async Task<string> CreateAndPublishSite(string name, Dictionary<string, string> files)
    {
        var createResponse = await _client.PostAsJsonAsync("/sites", new { SiteName = name });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = System.Text.Json.JsonDocument.Parse(createJson);
        var siteId = createDoc.RootElement.GetProperty("siteId").GetString()!;

        using var zip = CreateTestZip(files);
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(zip);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "site.zip");

        var publishResponse = await _client.PutAsync($"/sites/{siteId}/content", content);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return siteId;
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
}
