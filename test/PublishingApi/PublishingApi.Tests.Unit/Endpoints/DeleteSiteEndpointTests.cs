using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using PublishingApi.Domain.Interfaces;
using PublishingApi.Domain.Models;
using PublishingApi.Tests.Unit.Infrastructure;
using Xunit;

namespace PublishingApi.Tests.Unit.Endpoints;

public class DeleteSiteEndpointTests : IClassFixture<EndpointTestFixture>
{
    private readonly EndpointTestFixture _fixture;
    private const string TestSub = "user-a-sub-123";

    public DeleteSiteEndpointTests(EndpointTestFixture fixture)
    {
        _fixture = fixture;
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

    [Fact]
    public async Task Delete_WhenSiteNotFound_ReturnsNotFound()
    {
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, "nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Site?)null);

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.DeleteAsync("/sites/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithValidSite_DeletesAndReturnsNoContent()
    {
        var site = CreateTestSite();
        _fixture.SiteRepositoryMock.Reset();
        _fixture.StorageServiceMock.Reset();

        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, site.SiteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _fixture.StorageServiceMock
            .Setup(s => s.DeletePrefixAsync(site.ContentPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.DeleteAsync($"/sites/{site.SiteId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        _fixture.StorageServiceMock.Verify(s => s.DeletePrefixAsync(
            site.ContentPath, It.IsAny<CancellationToken>()), Times.Once);
        _fixture.SiteRepositoryMock.Verify(r => r.DeleteAsync(
            TestSub, site.SiteId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenS3DeleteFails_Returns500AndRetainsRecord()
    {
        var site = CreateTestSite();
        _fixture.SiteRepositoryMock.Reset();
        _fixture.StorageServiceMock.Reset();

        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, site.SiteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _fixture.StorageServiceMock
            .Setup(s => s.DeletePrefixAsync(site.ContentPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("S3 failure"));

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.DeleteAsync($"/sites/{site.SiteId}");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("retained");

        _fixture.SiteRepositoryMock.Verify(r => r.DeleteAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenDynamoDbDeleteFails_Returns500()
    {
        var site = CreateTestSite();
        _fixture.SiteRepositoryMock.Reset();
        _fixture.StorageServiceMock.Reset();

        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, site.SiteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _fixture.StorageServiceMock
            .Setup(s => s.DeletePrefixAsync(site.ContentPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _fixture.SiteRepositoryMock
            .Setup(r => r.DeleteAsync(TestSub, site.SiteId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DynamoDB failure"));

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.DeleteAsync($"/sites/{site.SiteId}");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().ContainEquivalentOf("manual");
    }
}
