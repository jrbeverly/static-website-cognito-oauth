using Amazon.CloudFront;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using PublishingApi.Api.Authorization;
using PublishingApi.Api.Endpoints.Sites;
using PublishingApi.Domain.Interfaces;
using PublishingApi.Infrastructure.Repositories;
using PublishingApi.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// Add environment variable overrides for Lambda compatibility.
// In Lambda, the colon separator used by .NET configuration sections (e.g. "CloudFront:Domain")
// is not valid in environment variable names, so Lambda defines them with double underscores
// (e.g. "CloudFront__Domain"). For variables that don't follow the double-underscore convention,
// merge them explicitly so both naming styles work.
var cloudFrontDomain = builder.Configuration["CloudFront:Domain"]
    ?? Environment.GetEnvironmentVariable("CLOUDFRONT_DOMAIN");
var cloudFrontDistributionId = builder.Configuration["CloudFront:DistributionId"]
    ?? Environment.GetEnvironmentVariable("CLOUDFRONT_DISTRIBUTION_ID");

if (!string.IsNullOrEmpty(cloudFrontDomain))
    builder.Configuration["CloudFront:Domain"] = cloudFrontDomain;
if (!string.IsNullOrEmpty(cloudFrontDistributionId))
    builder.Configuration["CloudFront:DistributionId"] = cloudFrontDistributionId;

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var config = new AmazonDynamoDBConfig();
    var serviceUrl = builder.Configuration["DynamoDB:ServiceUrl"];
    if (!string.IsNullOrEmpty(serviceUrl))
        config.ServiceURL = serviceUrl;
    return new AmazonDynamoDBClient(config);
});

builder.Services.AddSingleton<ISiteRepository>(sp =>
{
    var dynamoDb = sp.GetRequiredService<IAmazonDynamoDB>();
    var tableName = builder.Configuration["DynamoDB:TableName"]
        ?? Environment.GetEnvironmentVariable("DYNAMODB_TABLE")
        ?? "my-sites-staging";
    return new DynamoDbSiteRepository(dynamoDb, tableName);
});

builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var config = new AmazonS3Config();
    var serviceUrl = builder.Configuration["S3:ServiceUrl"];
    if (!string.IsNullOrEmpty(serviceUrl))
        config.ServiceURL = serviceUrl;
    return new AmazonS3Client(config);
});

builder.Services.AddSingleton<IAmazonCloudFront>(_ => new AmazonCloudFrontClient());

builder.Services.AddSingleton<ICloudFrontService>(sp =>
{
    var cloudFront = sp.GetRequiredService<IAmazonCloudFront>();
    return new CloudFrontInvalidationService(cloudFront);
});

builder.Services.AddSingleton<IStorageService>(sp =>
{
    var s3 = sp.GetRequiredService<IAmazonS3>();
    var bucketName = builder.Configuration["S3:BucketName"]
        ?? Environment.GetEnvironmentVariable("S3_BUCKET")
        ?? "my-sites-content-staging";
    return new S3StorageService(s3, bucketName);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var userPoolId = builder.Configuration["Cognito:UserPoolId"]
            ?? Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID");
        var region = builder.Configuration["Cognito:Region"]
            ?? Environment.GetEnvironmentVariable("COGNITO_REGION");

        options.Authority = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";
        options.Audience = builder.Configuration["Cognito:ClientId"];
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(ScopePolicies.SitesRead, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var scopeClaim = context.User.FindFirst("scope")?.Value;
            return scopeClaim is not null && scopeClaim.Split(' ').Contains(ScopePolicies.SitesRead);
        });
    })
    .AddPolicy(ScopePolicies.SitesWrite, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var scopeClaim = context.User.FindFirst("scope")?.Value;
            return scopeClaim is not null && scopeClaim.Split(' ').Contains(ScopePolicies.SitesWrite);
        });
    });

var app = builder.Build();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGetHealth();
app.MapCreateSite();
app.MapListSites();
app.MapGetSite();
app.MapDeleteSite();
app.MapPublishSiteContent();

app.Run();
