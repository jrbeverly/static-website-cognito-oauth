using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SystemTests.Infrastructure;

public class SystemTestClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public SystemTestClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<HttpResponseMessage> GetAsync(string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PostJsonAsync(string path, object body, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{path}")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PutZipAsync(
        string path, Stream zipStream, string fileName, string token)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(zipStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}{path}")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PutRawZipAsync(
        string path, Stream zipStream, string token)
    {
        var content = new StreamContent(zipStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}{path}")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request);
    }

    public static Stream CreateTestZip(Dictionary<string, string> files)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        ms.Position = 0;
        return ms;
    }
}
