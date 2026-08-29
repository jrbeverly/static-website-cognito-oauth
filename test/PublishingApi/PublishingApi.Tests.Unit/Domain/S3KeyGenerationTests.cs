using FluentAssertions;
using PublishingApi.Domain.Services;
using Xunit;

namespace PublishingApi.Tests.Unit.Domain;

public class S3KeyGenerationTests
{
    [Fact]
    public void GenerateContentPath_ReturnsPathWithTrailingSlash()
    {
        var result = S3KeyGenerator.GenerateContentPath("user-123", "site-abc");

        result.Should().Be("user-123/site-abc/");
    }

    [Fact]
    public void GenerateFileKey_ConcatenatesContentPathAndEntry()
    {
        var result = S3KeyGenerator.GenerateFileKey("user-123/site-abc/", "index.html");

        result.Should().Be("user-123/site-abc/index.html");
    }

    [Fact]
    public void GenerateFileKey_StripsLeadingSlashFromEntry()
    {
        var result = S3KeyGenerator.GenerateFileKey("user-123/site-abc/", "/styles/main.css");

        result.Should().Be("user-123/site-abc/styles/main.css");
    }

    [Fact]
    public void GenerateFileKey_ContentPathWithoutTrailingSlash_AddsSlash()
    {
        var result = S3KeyGenerator.GenerateFileKey("user-123/site-abc", "index.html");

        result.Should().Be("user-123/site-abc/index.html");
    }
}
