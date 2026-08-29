namespace PublishingApi.Infrastructure.Services;

public static class ContentTypeHelper
{
    private static readonly Dictionary<string, string> _contentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".htm"] = "text/html; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".js"] = "application/javascript; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".xml"] = "application/xml; charset=utf-8",
        [".txt"] = "text/plain; charset=utf-8",
        [".csv"] = "text/csv; charset=utf-8",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".ico"] = "image/x-icon",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".eot"] = "application/vnd.ms-fontobject",
        [".otf"] = "font/otf",
        [".pdf"] = "application/pdf",
        [".zip"] = "application/zip",
        [".wasm"] = "application/wasm",
    };

    public static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return _contentTypes.TryGetValue(ext, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
