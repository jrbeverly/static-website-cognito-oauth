using Amazon.S3;
using Amazon.S3.Model;
using PublishingApi.Domain.Interfaces;

namespace PublishingApi.Infrastructure.Repositories;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public S3StorageService(IAmazonS3 s3, string bucketName)
    {
        _s3 = s3;
        _bucketName = bucketName;
    }

    public async Task UploadObjectAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, ct);
        buffered.Position = 0;

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = buffered,
            ContentType = contentType
        };

        await _s3.PutObjectAsync(request, ct);
    }

    public async Task DeletePrefixAsync(string prefix, CancellationToken ct = default)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = prefix
        };

        ListObjectsV2Response listResponse;
        do
        {
            listResponse = await _s3.ListObjectsV2Async(listRequest, ct);

            if (listResponse.S3Objects.Count > 0)
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _bucketName,
                    Objects = listResponse.S3Objects
                        .Select(o => new KeyVersion { Key = o.Key })
                        .ToList()
                };

                await _s3.DeleteObjectsAsync(deleteRequest, ct);
            }

            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        }
        while (listResponse.IsTruncated);
    }
}
