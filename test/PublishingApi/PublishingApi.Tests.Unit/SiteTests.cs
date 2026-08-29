using FluentAssertions;
using PublishingApi.Domain.Models;
using Xunit;

namespace PublishingApi.Tests.Unit;

public class SiteTests
{
    [Fact]
    public void Site_Record_CanBeCreated_WithRequiredProperties()
    {
        var site = new Site
        {
            SiteId = "site-1",
            Sub = "user-1",
            SiteName = "my-blog",
            Status = "active",
            CreatedAt = "2026-06-04T10:00:00Z",
            UpdatedAt = "2026-06-04T10:00:00Z",
            ContentPath = "user-1/site-1/",
            SiteUrl = null
        };

        site.SiteId.Should().Be("site-1");
        site.Sub.Should().Be("user-1");
        site.SiteName.Should().Be("my-blog");
        site.Status.Should().Be("active");
        site.CreatedAt.Should().Be("2026-06-04T10:00:00Z");
        site.UpdatedAt.Should().Be("2026-06-04T10:00:00Z");
        site.ContentPath.Should().Be("user-1/site-1/");
        site.SiteUrl.Should().BeNull();
    }

    [Fact]
    public void Site_Record_CanBeCreated_WithSiteUrl()
    {
        var site = new Site
        {
            SiteId = "site-2",
            Sub = "user-1",
            SiteName = "docs",
            Status = "active",
            CreatedAt = "2026-06-04T10:00:00Z",
            UpdatedAt = "2026-06-04T10:00:00Z",
            ContentPath = "user-1/site-2/",
            SiteUrl = "https://cdn.example.com/user-1/site-2/"
        };

        site.SiteUrl.Should().Be("https://cdn.example.com/user-1/site-2/");
    }
}
