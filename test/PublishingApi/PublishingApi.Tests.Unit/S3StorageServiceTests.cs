using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Moq;
using PublishingApi.Infrastructure.Repositories;
using Xunit;

namespace PublishingApi.Tests.Unit;

public class S3StorageServiceTests
{
    private readonly Mock<IAmazonS3> _s3Mock;
    private readonly S3StorageService _service;

    public S3StorageServiceTests()
    {
        _s3Mock = new Mock<IAmazonS3>();
        _service = new S3StorageService(_s3Mock.Object, "test-bucket");
    }

    [Fact]
    public async Task UploadObjectAsync_PutsObjectWithCorrectKeyAndContentType()
    {
        var content = Encoding.UTF8.GetBytes("<html></html>");
        using var stream = new MemoryStream(content);

        _s3Mock
            .Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ReturnsAsync(new PutObjectResponse());

        await _service.UploadObjectAsync("abc123/site-1/index.html", stream, "text/html; charset=utf-8");

        _s3Mock.Verify(s => s.PutObjectAsync(
            It.Is<PutObjectRequest>(r =>
                r.BucketName == "test-bucket" &&
                r.Key == "abc123/site-1/index.html" &&
                r.ContentType == "text/html; charset=utf-8"
            ),
            default), Times.Once);
    }

    [Fact]
    public async Task DeletePrefixAsync_DeletesAllObjectsUnderPrefix()
    {
        var objects = new List<S3Object>
        {
            new() { Key = "abc123/site-1/index.html" },
            new() { Key = "abc123/site-1/css/style.css" }
        };

        _s3Mock
            .Setup(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(), default))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = objects,
                IsTruncated = false
            });

        _s3Mock
            .Setup(s => s.DeleteObjectsAsync(
                It.IsAny<DeleteObjectsRequest>(), default))
            .ReturnsAsync(new DeleteObjectsResponse());

        await _service.DeletePrefixAsync("abc123/site-1/");

        _s3Mock.Verify(s => s.ListObjectsV2Async(
            It.Is<ListObjectsV2Request>(r =>
                r.BucketName == "test-bucket" &&
                r.Prefix == "abc123/site-1/"
            ),
            default), Times.Once);

        _s3Mock.Verify(s => s.DeleteObjectsAsync(
            It.Is<DeleteObjectsRequest>(r =>
                r.BucketName == "test-bucket" &&
                r.Objects.Count == 2 &&
                r.Objects.Any(o => o.Key == "abc123/site-1/index.html") &&
                r.Objects.Any(o => o.Key == "abc123/site-1/css/style.css")
            ),
            default), Times.Once);
    }

    [Fact]
    public async Task DeletePrefixAsync_NoObjects_DoesNotCallDelete()
    {
        _s3Mock
            .Setup(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(), default))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [],
                IsTruncated = false
            });

        await _service.DeletePrefixAsync("empty-prefix/");

        _s3Mock.Verify(s => s.DeleteObjectsAsync(
            It.IsAny<DeleteObjectsRequest>(), default), Times.Never);
    }

    [Fact]
    public async Task DeletePrefixAsync_PaginatesWhenTruncated()
    {
        var page1 = new List<S3Object>
        {
            new() { Key = "abc123/site-1/file1.html" }
        };
        var page2 = new List<S3Object>
        {
            new() { Key = "abc123/site-1/file2.html" }
        };

        _s3Mock
            .SetupSequence(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(), default))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = page1,
                IsTruncated = true,
                NextContinuationToken = "token-1"
            })
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = page2,
                IsTruncated = false
            });

        _s3Mock
            .Setup(s => s.DeleteObjectsAsync(
                It.IsAny<DeleteObjectsRequest>(), default))
            .ReturnsAsync(new DeleteObjectsResponse());

        await _service.DeletePrefixAsync("abc123/site-1/");

        _s3Mock.Verify(s => s.ListObjectsV2Async(
            It.IsAny<ListObjectsV2Request>(), default), Times.Exactly(2));
        _s3Mock.Verify(s => s.DeleteObjectsAsync(
            It.IsAny<DeleteObjectsRequest>(), default), Times.Exactly(2));
    }
}
