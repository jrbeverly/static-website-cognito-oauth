using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using PublishingApi.Domain.Interfaces;

namespace PublishingApi.Infrastructure.Services;

public class CloudFrontInvalidationService : ICloudFrontService
{
    private readonly IAmazonCloudFront _cloudFront;

    public CloudFrontInvalidationService(IAmazonCloudFront cloudFront)
    {
        _cloudFront = cloudFront;
    }

    public async Task InvalidateAsync(string distributionId, string path, CancellationToken ct = default)
    {
        var request = new CreateInvalidationRequest
        {
            DistributionId = distributionId,
            InvalidationBatch = new InvalidationBatch
            {
                Paths = new Paths
                {
                    Quantity = 1,
                    Items = [path]
                },
                CallerReference = $"{DateTime.UtcNow.Ticks}"
            }
        };

        await _cloudFront.CreateInvalidationAsync(request, ct);
    }
}
