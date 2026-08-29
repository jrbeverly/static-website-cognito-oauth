using FluentAssertions;
using Xunit;

namespace PublishingApi.Tests.Unit;

public class CloudFrontFunctionTests
{
    [Fact]
    public void DirectoryPath_WithTrailingSlash_AppendsIndexHtml()
    {
        var result = RewriteUri("/test-user-abc123/test-site-001/");
        result.Should().Be("/test-user-abc123/test-site-001/index.html");
    }

    [Fact]
    public void ExplicitIndexHtml_ReturnsUnchanged()
    {
        var result = RewriteUri("/test-user-abc123/test-site-001/index.html");
        result.Should().Be("/test-user-abc123/test-site-001/index.html");
    }

    [Fact]
    public void AssetFile_WithExtension_ReturnsUnchanged()
    {
        var result = RewriteUri("/test-user-abc123/test-site-001/assets/style.css");
        result.Should().Be("/test-user-abc123/test-site-001/assets/style.css");
    }

    [Fact]
    public void NonExistentFile_WithExtension_ReturnsUnchanged()
    {
        var result = RewriteUri("/test-user-abc123/test-site-001/notfound.html");
        result.Should().Be("/test-user-abc123/test-site-001/notfound.html");
    }

    [Fact]
    public void ExtensionlessPath_AppendsSlashIndexHtml()
    {
        var result = RewriteUri("/test-user-abc123/test-site-001/subpath");
        result.Should().Be("/test-user-abc123/test-site-001/subpath/index.html");
    }

    [Fact]
    public void RootPath_AppendsIndexHtml()
    {
        var result = RewriteUri("/");
        result.Should().Be("/index.html");
    }

    [Fact]
    public void OtherUserNamespace_WithTrailingSlash_AppendsIndexHtml()
    {
        var result = RewriteUri("/other-user/test-site-001/");
        result.Should().Be("/other-user/test-site-001/index.html");
    }

    [Fact]
    public void FileInRoot_WithExtension_ReturnsUnchanged()
    {
        var result = RewriteUri("/favicon.ico");
        result.Should().Be("/favicon.ico");
    }

    [Fact]
    public void SingularPath_NoExtension_AppendsSlashIndexHtml()
    {
        var result = RewriteUri("/foo");
        result.Should().Be("/foo/index.html");
    }

    [Fact]
    public void DeepAssetFile_WithExtension_ReturnsUnchanged()
    {
        var result = RewriteUri("/user-123/site-456/js/bundle.abc123.js");
        result.Should().Be("/user-123/site-456/js/bundle.abc123.js");
    }

    [Fact]
    public void QueryParams_InUri_PreservesQueryString()
    {
        var result = RewriteUri("/test-user-abc123/test-site-001/?v=1");
        result.Should().Be("/test-user-abc123/test-site-001/index.html?v=1");
    }

    [Fact]
    public void FileWithDotInPath_StillDetectsExtensionCorrectly()
    {
        var result = RewriteUri("/user-123/my.blog.site/about");
        result.Should().Be("/user-123/my.blog.site/about/index.html");
    }

    private static string RewriteUri(string uri)
    {
        // Equivalent logic to modules/cloudfront-serving/cloudfront-function.js
        string query = "";
        var queryIndex = uri.IndexOf('?');
        if (queryIndex >= 0)
        {
            query = uri[queryIndex..];
            uri = uri[..queryIndex];
        }

        if (uri.EndsWith('/'))
            uri += "index.html";
        else
        {
            var lastSlash = Math.Max(uri.LastIndexOf('/'), 0);
            if (uri.IndexOf('.', lastSlash) < 0)
                uri += "/index.html";
        }

        return uri + query;
    }
}
