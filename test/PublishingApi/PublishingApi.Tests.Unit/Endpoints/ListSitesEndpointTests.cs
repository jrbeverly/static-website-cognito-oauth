using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using PublishingApi.Domain.Models;
using PublishingApi.Tests.Unit.Infrastructure;
using Xunit;

namespace PublishingApi.Tests.Unit.Endpoints;

public class ListSitesEndpointTests : IClassFixture<EndpointTestFixture>
{
    private readonly EndpointTestFixture _fixture;
    private const string TestSub = "user-a-sub-123";

    public ListSitesEndpointTests(EndpointTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ListSites_WithSites_ReturnsOkWithSiteList()
    {
        var sites = new List<Site>
        {
            new()
            {
                SiteId = "site-1",
                Sub = TestSub,
                SiteName = "Site One",
                Status = "active",
                CreatedAt = "2026-06-04T10:00:00Z",
                UpdatedAt = "2026-06-04T10:00:00Z",
                ContentPath = $"{TestSub}/site-1/",
                SiteUrl = "https://cdn.example.com/site-1"
            },
            new()
            {
                SiteId = "site-2",
                Sub = TestSub,
                SiteName = "Site Two",
                Status = "active",
                CreatedAt = "2026-06-04T11:00:00Z",
                UpdatedAt = "2026-06-04T11:00:00Z",
                ContentPath = $"{TestSub}/site-2/",
                SiteUrl = null
            }
        };

        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.ListByUserAsync(TestSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sites);

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.GetAsync("/sites");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.EnumerateArray().ToList();
        items.Should().HaveCount(2);
        items[0].GetProperty("siteId").GetString().Should().Be("site-1");
        items[0].GetProperty("siteName").GetString().Should().Be("Site One");
        items[1].GetProperty("siteId").GetString().Should().Be("site-2");
        items[1].GetProperty("siteName").GetString().Should().Be("Site Two");
        items[1].TryGetProperty("siteUrl", out var url).Should().BeTrue();
        url.GetString().Should().BeNull();
    }

    [Fact]
    public async Task ListSites_WithNoSites_ReturnsOkWithEmptyArray()
    {
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.ListByUserAsync(TestSub, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Site>());

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.GetAsync("/sites");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListSites_WhenRepositoryThrows_ReturnsInternalServerError()
    {
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.ListByUserAsync(TestSub, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.GetAsync("/sites");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
