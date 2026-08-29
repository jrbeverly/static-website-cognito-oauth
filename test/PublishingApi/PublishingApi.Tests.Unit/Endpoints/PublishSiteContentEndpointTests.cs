using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Moq;
using PublishingApi.Domain.Interfaces;
using PublishingApi.Domain.Models;
using PublishingApi.Tests.Unit.Infrastructure;
using Xunit;

namespace PublishingApi.Tests.Unit.Endpoints;

public class PublishSiteContentEndpointTests : IClassFixture<EndpointTestFixture>
{
    private readonly EndpointTestFixture _fixture;
    private const string TestSub = "user-a-sub-123";

    public PublishSiteContentEndpointTests(EndpointTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Publish_WhenSiteNotFound_ReturnsNotFound()
    {
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, "nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Site?)null);

        using var client = _fixture.CreateClient(TestSub);
        using var content = CreateZipContent(new Dictionary<string, string>
        {
            ["index.html"] = "<html></html>"
        });

        var response = await client.PutAsync("/sites/nonexistent/content", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publish_WithValidZip_UploadsFilesAndReturnsOk()
    {
        var site = CreateTestSite();
        _fixture.SiteRepositoryMock.Reset();
        _fixture.StorageServiceMock.Reset();
        _fixture.CloudFrontServiceMock.Reset();

        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, site.SiteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _fixture.StorageServiceMock
            .Setup(s => s.UploadObjectAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _fixture.StorageServiceMock
            .Setup(s => s.DeletePrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _fixture.CloudFrontServiceMock
            .Setup(c => c.InvalidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = _fixture.CreateClient(TestSub);
        using var content = CreateZipContent(new Dictionary<string, string>
        {
            ["index.html"] = "<html><body>Hello</body></html>",
            ["css/style.css"] = "body { color: red; }"
        });

        var response = await client.PutAsync($"/sites/{site.SiteId}/content", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _fixture.StorageServiceMock.Verify(s => s.DeletePrefixAsync(
            site.ContentPath, It.IsAny<CancellationToken>()), Times.Once);

        _fixture.StorageServiceMock.Verify(s => s.UploadObjectAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _fixture.CloudFrontServiceMock.Verify(c => c.InvalidateAsync(
            "E123TEST", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_WhenCloudFrontFails_StillReturnsOk()
    {
        var site = CreateTestSite();
        _fixture.SiteRepositoryMock.Reset();
        _fixture.StorageServiceMock.Reset();
        _fixture.CloudFrontServiceMock.Reset();

        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, site.SiteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _fixture.StorageServiceMock
            .Setup(s => s.UploadObjectAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _fixture.StorageServiceMock
            .Setup(s => s.DeletePrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _fixture.CloudFrontServiceMock
            .Setup(c => c.InvalidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("CloudFront unavailable"));

        using var client = _fixture.CreateClient(TestSub);
        using var content = CreateZipContent(new Dictionary<string, string>
        {
            ["index.html"] = "<html></html>"
        });

        var response = await client.PutAsync($"/sites/{site.SiteId}/content", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static Site CreateTestSite()
    {
        return new Site
        {
            SiteId = "site-1",
            Sub = TestSub,
            SiteName = "test-site",
            Status = "active",
            CreatedAt = "2026-06-04T10:00:00Z",
            UpdatedAt = "2026-06-04T10:00:00Z",
            ContentPath = $"{TestSub}/site-1/",
            SiteUrl = null
        };
    }

    private static HttpContent CreateZipContent(Dictionary<string, string> files)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, fileContent) in files)
            {
                var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(fileContent);
            }
        }
        ms.Position = 0;

        var content = new MultipartFormDataContent();
        var fileContent2 = new StreamContent(ms);
        fileContent2.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent2, "file", "site.zip");
        return content;
    }
}
