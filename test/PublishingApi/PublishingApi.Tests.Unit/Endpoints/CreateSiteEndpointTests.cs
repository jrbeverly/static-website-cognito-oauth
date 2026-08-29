using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Moq;
using PublishingApi.Domain.Models;
using PublishingApi.Tests.Unit.Infrastructure;
using Xunit;

namespace PublishingApi.Tests.Unit.Endpoints;

public class CreateSiteEndpointTests : IClassFixture<EndpointTestFixture>
{
    private readonly EndpointTestFixture _fixture;
    private const string TestSub = "user-a-sub-123";

    public CreateSiteEndpointTests(EndpointTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateSite_WithEmptySiteName_ReturnsBadRequest()
    {
        _fixture.SiteRepositoryMock.Reset();

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.PostAsJsonAsync("/sites", new { SiteName = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSite_WithValidSiteName_CallsRepositoryWithCorrectValues()
    {
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Site>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Site s, CancellationToken _) => s);

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.PostAsJsonAsync("/sites", new { SiteName = "my-blog" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        _fixture.SiteRepositoryMock.Verify(r => r.CreateAsync(
            It.Is<Site>(s =>
                s.Sub == TestSub &&
                s.SiteName == "my-blog" &&
                s.Status == "active" &&
                s.ContentPath.StartsWith($"{TestSub}/") &&
                s.ContentPath.EndsWith("/")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateSite_WhenRepositoryThrows_ReturnsInternalServerError()
    {
        _fixture.SiteRepositoryMock.Reset();
        _fixture.SiteRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Site>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failure"));

        using var client = _fixture.CreateClient(TestSub);
        var response = await client.PostAsJsonAsync("/sites", new { SiteName = "my-blog" });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
