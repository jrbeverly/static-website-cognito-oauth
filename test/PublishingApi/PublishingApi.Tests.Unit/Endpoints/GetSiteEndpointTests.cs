using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using PublishingApi.Domain.Models;
using PublishingApi.Tests.Unit.Infrastructure;
using Xunit;

namespace PublishingApi.Tests.Unit.Endpoints;

public class GetSiteEndpointTests : IClassFixture<EndpointTestFixture>
{
    private readonly EndpointTestFixture _fixture;
    private const string TestSub = "user-a-sub-123";

    public GetSiteEndpointTests(EndpointTestFixture fixture)
    {
        _fixture = fixture;
    }

    private static Site CreateTestSite(string siteId = "site-1")
    {
        return new Site
        {
            SiteId = siteId,
            Sub = TestSub,
            SiteName = "test-site",
            Status = "active",
            CreatedAt = "2026-06-04T10:00:00Z",
            UpdatedAt = "2026-06-04T10:00:00Z",
            ContentPath = $"{TestSub}/{siteId}/",
            SiteUrl = $"https://cdn.example.com/{TestSub}/{siteId}/"
        };
    }

    [Fact]
    public async Task GetSite_WhenSiteExists_ReturnsOkWithSiteDetails()
    {
        var site = CreateTestSite();
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, site.SiteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.GetAsync($"/sites/{site.SiteId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("siteId").GetString().Should().Be(site.SiteId);
        doc.RootElement.GetProperty("siteName").GetString().Should().Be(site.SiteName);
        doc.RootElement.GetProperty("status").GetString().Should().Be(site.Status);
        doc.RootElement.GetProperty("contentPath").GetString().Should().Be(site.ContentPath);
        doc.RootElement.GetProperty("siteUrl").GetString().Should().Be(site.SiteUrl);
    }

    [Fact]
    public async Task GetSite_WhenSiteNotFound_ReturnsNotFound()
    {
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, "nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Site?)null);

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.GetAsync("/sites/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSite_WhenRepositoryThrows_ReturnsInternalServerError()
    {
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.GetByIdAsync(TestSub, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.GetAsync("/sites/site-1");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
