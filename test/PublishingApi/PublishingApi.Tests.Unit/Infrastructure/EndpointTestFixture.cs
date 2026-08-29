using Amazon.CloudFront;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using PublishingApi.Api;
using PublishingApi.Domain.Interfaces;
using Xunit;

namespace PublishingApi.Tests.Unit.Infrastructure;

public class EndpointTestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;

    public Mock<ISiteRepository> SiteRepositoryMock { get; } = new();
    public Mock<IStorageService> StorageServiceMock { get; } = new();
    public Mock<ICloudFrontService> CloudFrontServiceMock { get; } = new();

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["DynamoDB:ServiceUrl"] = "http://localhost:8000",
                        ["DynamoDB:TableName"] = "test-table",
                        ["S3:ServiceUrl"] = "http://localhost:9000",
                        ["S3:BucketName"] = "test-bucket",
                        ["CloudFront:Domain"] = "cdn.example.com",
                        ["CloudFront:DistributionId"] = "E123TEST",
                        ["Cognito:UserPoolId"] = "us-east-1_TEST",
                        ["Cognito:Region"] = "us-east-1",
                        ["Cognito:ClientId"] = "test-client-id"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IAmazonDynamoDB>();
                    services.RemoveAll<IAmazonS3>();
                    services.RemoveAll<IAmazonCloudFront>();
                    services.RemoveAll<ISiteRepository>();
                    services.RemoveAll<IStorageService>();
                    services.RemoveAll<ICloudFrontService>();

                    services.AddSingleton(new Mock<IAmazonDynamoDB>().Object);
                    services.AddSingleton(new Mock<IAmazonS3>().Object);
                    services.AddSingleton(new Mock<IAmazonCloudFront>().Object);
                    services.AddSingleton(SiteRepositoryMock.Object);
                    services.AddSingleton(StorageServiceMock.Object);
                    services.AddSingleton(CloudFrontServiceMock.Object);

                    services.AddAuthentication(defaultScheme: TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });
                });
            });

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
        await Task.CompletedTask;
    }

    public HttpClient CreateClient(string testUser = "user-a-sub-123")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", testUser);
        return client;
    }
}
