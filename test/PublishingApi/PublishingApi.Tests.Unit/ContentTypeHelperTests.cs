using FluentAssertions;
using PublishingApi.Infrastructure.Services;
using Xunit;

namespace PublishingApi.Tests.Unit;

public class ContentTypeHelperTests
{
    [Theory]
    [InlineData("index.html", "text/html; charset=utf-8")]
    [InlineData("page.htm", "text/html; charset=utf-8")]
    [InlineData("styles.css", "text/css; charset=utf-8")]
    [InlineData("app.js", "application/javascript; charset=utf-8")]
    [InlineData("data.json", "application/json; charset=utf-8")]
    [InlineData("config.xml", "application/xml; charset=utf-8")]
    [InlineData("logo.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("icon.gif", "image/gif")]
    [InlineData("hero.webp", "image/webp")]
    [InlineData("favicon.ico", "image/x-icon")]
    [InlineData("font.woff", "font/woff")]
    [InlineData("font.woff2", "font/woff2")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    public void GetContentType_ReturnsCorrectMimeType(string fileName, string expected)
    {
        ContentTypeHelper.GetContentType(fileName).Should().Be(expected);
    }
}
