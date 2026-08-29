using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using FluentAssertions;
using Moq;
using PublishingApi.Infrastructure.Services;
using Xunit;

namespace PublishingApi.Tests.Unit;

public class CloudFrontInvalidationServiceTests
{
    private readonly Mock<IAmazonCloudFront> _cloudFrontMock;
    private readonly CloudFrontInvalidationService _service;

    public CloudFrontInvalidationServiceTests()
    {
        _cloudFrontMock = new Mock<IAmazonCloudFront>();
        _service = new CloudFrontInvalidationService(_cloudFrontMock.Object);
    }

    [Fact]
    public async Task InvalidateAsync_CreatesInvalidationWithCorrectPath()
    {
        _cloudFrontMock
            .Setup(c => c.CreateInvalidationAsync(It.IsAny<CreateInvalidationRequest>(), default))
            .ReturnsAsync(new CreateInvalidationResponse());

        await _service.InvalidateAsync("E123ABC", "/abc123/site-1/*");

        _cloudFrontMock.Verify(c => c.CreateInvalidationAsync(
            It.Is<CreateInvalidationRequest>(r =>
                r.DistributionId == "E123ABC" &&
                r.InvalidationBatch.Paths.Quantity == 1 &&
                r.InvalidationBatch.Paths.Items[0] == "/abc123/site-1/*"
            ),
            default), Times.Once);
    }
}
