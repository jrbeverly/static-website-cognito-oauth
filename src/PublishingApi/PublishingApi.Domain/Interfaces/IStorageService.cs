namespace PublishingApi.Domain.Interfaces;

public interface IStorageService
{
    Task UploadObjectAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task DeletePrefixAsync(string prefix, CancellationToken ct = default);
}
