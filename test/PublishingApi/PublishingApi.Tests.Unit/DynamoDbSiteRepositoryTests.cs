using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Moq;
using PublishingApi.Domain.Models;
using PublishingApi.Infrastructure.Repositories;
using Xunit;

namespace PublishingApi.Tests.Unit;

public class DynamoDbSiteRepositoryTests
{
    private readonly Mock<IAmazonDynamoDB> _dynamoDbMock;
    private readonly DynamoDbSiteRepository _repository;

    public DynamoDbSiteRepositoryTests()
    {
        _dynamoDbMock = new Mock<IAmazonDynamoDB>();
        _repository = new DynamoDbSiteRepository(_dynamoDbMock.Object, "test-table");
    }

    [Fact]
    public async Task CreateAsync_PutsItemWithCorrectKeys()
    {
        var site = CreateTestSite();

        _dynamoDbMock
            .Setup(d => d.PutItemAsync(It.IsAny<PutItemRequest>(), default))
            .ReturnsAsync(new PutItemResponse());

        var result = await _repository.CreateAsync(site);

        _dynamoDbMock.Verify(d => d.PutItemAsync(
            It.Is<PutItemRequest>(r =>
                r.TableName == "test-table" &&
                r.Item["PK"].S == "USER#abc123" &&
                r.Item["SK"].S == "SITE#site-1" &&
                r.Item["SiteId"].S == "site-1" &&
                r.Item["SiteName"].S == "my-blog" &&
                r.Item["Status"].S == "active" &&
                r.ConditionExpression == "attribute_not_exists(PK) AND attribute_not_exists(SK)"
            ),
            default), Times.Once);

        result.Should().BeEquivalentTo(site);
    }

    [Fact]
    public async Task ListByUserAsync_QueriesWithCorrectKeys()
    {
        var site = CreateTestSite();
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = "USER#abc123" },
            ["SK"] = new AttributeValue { S = "SITE#site-1" },
            ["SiteId"] = new AttributeValue { S = "site-1" },
            ["Sub"] = new AttributeValue { S = "abc123" },
            ["SiteName"] = new AttributeValue { S = "my-blog" },
            ["Status"] = new AttributeValue { S = "active" },
            ["CreatedAt"] = new AttributeValue { S = "2026-06-04T10:00:00Z" },
            ["UpdatedAt"] = new AttributeValue { S = "2026-06-04T10:00:00Z" },
            ["ContentPath"] = new AttributeValue { S = "abc123/site-1/" }
        };

        _dynamoDbMock
            .Setup(d => d.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new QueryResponse { Items = [item] });

        var result = await _repository.ListByUserAsync("abc123");

        _dynamoDbMock.Verify(d => d.QueryAsync(
            It.Is<QueryRequest>(r =>
                r.KeyConditionExpression == "PK = :pk AND begins_with(SK, :sk)" &&
                r.ExpressionAttributeValues[":pk"].S == "USER#abc123" &&
                r.ExpressionAttributeValues[":sk"].S == "SITE#" &&
                r.FilterExpression == "#status = :status" &&
                r.ExpressionAttributeValues[":status"].S == "active"
            ),
            default), Times.Once);

        result.Should().HaveCount(1);
        result[0].SiteId.Should().Be("site-1");
        result[0].SiteName.Should().Be("my-blog");
    }

    [Fact]
    public async Task ListByUserAsync_ReturnsEmptyList_WhenNoSites()
    {
        _dynamoDbMock
            .Setup(d => d.QueryAsync(It.IsAny<QueryRequest>(), default))
            .ReturnsAsync(new QueryResponse { Items = [] });

        var result = await _repository.ListByUserAsync("nobody");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSite_WhenFound()
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = "USER#abc123" },
            ["SK"] = new AttributeValue { S = "SITE#site-1" },
            ["SiteId"] = new AttributeValue { S = "site-1" },
            ["Sub"] = new AttributeValue { S = "abc123" },
            ["SiteName"] = new AttributeValue { S = "my-blog" },
            ["Status"] = new AttributeValue { S = "active" },
            ["CreatedAt"] = new AttributeValue { S = "2026-06-04T10:00:00Z" },
            ["UpdatedAt"] = new AttributeValue { S = "2026-06-04T10:00:00Z" },
            ["ContentPath"] = new AttributeValue { S = "abc123/site-1/" }
        };

        _dynamoDbMock
            .Setup(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), default))
            .ReturnsAsync(new GetItemResponse { Item = item });

        var result = await _repository.GetByIdAsync("abc123", "site-1");

        _dynamoDbMock.Verify(d => d.GetItemAsync(
            It.Is<GetItemRequest>(r =>
                r.Key["PK"].S == "USER#abc123" &&
                r.Key["SK"].S == "SITE#site-1"
            ),
            default), Times.Once);

        result.Should().NotBeNull();
        result!.SiteId.Should().Be("site-1");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _dynamoDbMock
            .Setup(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), default))
            .ReturnsAsync(new GetItemResponse { Item = [] });

        var result = await _repository.GetByIdAsync("abc123", "nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenSubDoesNotMatch()
    {
        // Item exists but belongs to a different user
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = "USER#abc123" },
            ["SK"] = new AttributeValue { S = "SITE#site-1" },
            ["SiteId"] = new AttributeValue { S = "site-1" },
            ["Sub"] = new AttributeValue { S = "def456" },
            ["SiteName"] = new AttributeValue { S = "my-blog" },
            ["Status"] = new AttributeValue { S = "active" },
            ["CreatedAt"] = new AttributeValue { S = "2026-06-04T10:00:00Z" },
            ["UpdatedAt"] = new AttributeValue { S = "2026-06-04T10:00:00Z" },
            ["ContentPath"] = new AttributeValue { S = "abc123/site-1/" }
        };

        _dynamoDbMock
            .Setup(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), default))
            .ReturnsAsync(new GetItemResponse { Item = item });

        var result = await _repository.GetByIdAsync("abc123", "site-1");

        // Sub in item is "def456" but requested sub is "abc123" → mismatch
        result.Should().BeNull();
    }

    private static Site CreateTestSite()
    {
        return new Site
        {
            SiteId = "site-1",
            Sub = "abc123",
            SiteName = "my-blog",
            Status = "active",
            CreatedAt = "2026-06-04T10:00:00Z",
            UpdatedAt = "2026-06-04T10:00:00Z",
            ContentPath = "abc123/site-1/",
            SiteUrl = null
        };
    }

    [Fact]
    public async Task DeleteAsync_DeletesItemWithCorrectKeys()
    {
        _dynamoDbMock
            .Setup(d => d.DeleteItemAsync(It.IsAny<DeleteItemRequest>(), default))
            .ReturnsAsync(new DeleteItemResponse());

        await _repository.DeleteAsync("abc123", "site-1");

        _dynamoDbMock.Verify(d => d.DeleteItemAsync(
            It.Is<DeleteItemRequest>(r =>
                r.TableName == "test-table" &&
                r.Key["PK"].S == "USER#abc123" &&
                r.Key["SK"].S == "SITE#site-1"
            ),
            default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSiteUrlAndUpdatedAt()
    {
        _dynamoDbMock
            .Setup(d => d.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), default))
            .ReturnsAsync(new UpdateItemResponse());

        await _repository.UpdateAsync("abc123", "site-1",
            "https://cdn.example.com/abc123/site-1/", "2026-06-04T12:00:00Z");

        _dynamoDbMock.Verify(d => d.UpdateItemAsync(
            It.Is<UpdateItemRequest>(r =>
                r.TableName == "test-table" &&
                r.Key["PK"].S == "USER#abc123" &&
                r.Key["SK"].S == "SITE#site-1" &&
                r.UpdateExpression == "SET SiteUrl = :url, UpdatedAt = :updated" &&
                r.ExpressionAttributeValues[":url"].S == "https://cdn.example.com/abc123/site-1/" &&
                r.ExpressionAttributeValues[":updated"].S == "2026-06-04T12:00:00Z"
            ),
            default), Times.Once);
    }
}
