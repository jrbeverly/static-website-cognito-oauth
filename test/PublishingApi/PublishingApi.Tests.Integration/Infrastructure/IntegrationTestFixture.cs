using Amazon.DynamoDBv2;
using Amazon.S3;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace PublishingApi.Tests.Integration.Infrastructure;

public class IntegrationTestFixture : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;

    public AmazonDynamoDBClient DynamoDb { get; private set; } = null!;
    public AmazonS3Client S3 { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        await _factory.StartAsync();
        DynamoDb = _factory.DynamoDb;
        S3 = _factory.S3;
    }

    public async Task DisposeAsync()
    {
        DynamoDb?.Dispose();
        S3?.Dispose();
        await _factory.StopAsync();
        _factory.Dispose();
    }

    public HttpClient CreateClient() => _factory.CreateClient();

    public HttpClient CreateClient(Action<IWebHostBuilder> configure) =>
        _factory.WithWebHostBuilder(configure).CreateClient();
}
