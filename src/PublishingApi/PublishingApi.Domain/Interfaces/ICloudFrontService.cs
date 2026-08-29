namespace PublishingApi.Domain.Interfaces;

public interface ICloudFrontService
{
    Task InvalidateAsync(string distributionId, string path, CancellationToken ct = default);
}
