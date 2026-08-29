namespace PublishingApi.Domain.Services;

public static class S3KeyGenerator
{
    public static string GenerateContentPath(string sub, string siteId)
    {
        return $"{sub}/{siteId}/";
    }

    public static string GenerateFileKey(string contentPath, string entryName)
    {
        var trimmed = entryName.TrimStart('/');
        return contentPath.EndsWith('/')
            ? $"{contentPath}{trimmed}"
            : $"{contentPath}/{trimmed}";
    }
}
