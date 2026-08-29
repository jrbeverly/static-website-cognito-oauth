using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PublishingApi.Api;

namespace PublishingApi.Tests.Integration.Infrastructure;

internal class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IContainer _localStack;

    private const string TableName = "my-sites-test";
    private const string BucketName = "my-sites-content-test";
    private const int LocalStackPort = 4566;

    public AmazonDynamoDBClient DynamoDb { get; private set; } = null!;
    public AmazonS3Client S3 { get; private set; } = null!;
    public string LocalStackEndpoint { get; private set; } = null!;

    public CustomWebApplicationFactory()
    {
        _localStack = new ContainerBuilder()
            .WithImage("localstack/localstack:3.8")
            .WithPortBinding(LocalStackPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(LocalStackPort))
            .WithEnvironment("SERVICES", "s3,dynamodb")
            .Build();
    }

    public async Task StartAsync()
    {
        await _localStack.StartAsync();
        LocalStackEndpoint = $"http://{_localStack.Hostname}:{_localStack.GetMappedPublicPort(LocalStackPort)}";

        var credentials = new BasicAWSCredentials("test", "test");

        DynamoDb = new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig
        {
            ServiceURL = LocalStackEndpoint
        });

        S3 = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = LocalStackEndpoint,
            ForcePathStyle = true
        });

        await CreateTestTableAsync();
        await CreateTestBucketAsync();
    }

    public async Task StopAsync()
    {
        DynamoDb?.Dispose();
        S3?.Dispose();
        await _localStack.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DynamoDB:ServiceUrl"] = LocalStackEndpoint,
                ["DynamoDB:TableName"] = TableName,
                ["S3:ServiceUrl"] = LocalStackEndpoint,
                ["S3:BucketName"] = BucketName,
                ["CloudFront:Domain"] = "cdn.example.com",
                ["Cognito:UserPoolId"] = "us-east-1_TestPool",
                ["Cognito:Region"] = "us-east-1",
                ["Cognito:ClientId"] = "test-client-id"
            });
        });

        builder.ConfigureServices(services =>
        {
            var credentials = new BasicAWSCredentials("test", "test");

            var dynamoDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAmazonDynamoDB));
            if (dynamoDescriptor is not null)
                services.Remove(dynamoDescriptor);

            services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(
                credentials,
                new AmazonDynamoDBConfig { ServiceURL = LocalStackEndpoint }));

            var s3Descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(AmazonS3Client) ||
                     d.ServiceType == typeof(IAmazonS3));
            if (s3Descriptor is not null)
                services.Remove(s3Descriptor);

            services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                credentials,
                new AmazonS3Config
                {
                    ServiceURL = LocalStackEndpoint,
                    ForcePathStyle = true
                }));
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(defaultScheme: TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });
    }

    private async Task CreateTestTableAsync()
    {
        var request = new CreateTableRequest
        {
            TableName = TableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema =
            [
                new KeySchemaElement("PK", KeyType.HASH),
                new KeySchemaElement("SK", KeyType.RANGE)
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition("PK", ScalarAttributeType.S),
                new AttributeDefinition("SK", ScalarAttributeType.S)
            ]
        };

        await DynamoDb.CreateTableAsync(request);
    }

    private async Task CreateTestBucketAsync()
    {
        await S3.PutBucketAsync(new PutBucketRequest
        {
            BucketName = BucketName
        });
    }
}
