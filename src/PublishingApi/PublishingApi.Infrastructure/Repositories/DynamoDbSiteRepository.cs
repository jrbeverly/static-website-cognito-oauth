using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using PublishingApi.Domain.Interfaces;
using PublishingApi.Domain.Models;

namespace PublishingApi.Infrastructure.Repositories;

public class DynamoDbSiteRepository : ISiteRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbSiteRepository(IAmazonDynamoDB dynamoDb, string tableName)
    {
        _dynamoDb = dynamoDb;
        _tableName = tableName;
    }

    public async Task<Site> CreateAsync(Site site, CancellationToken ct = default)
    {
        var item = ToAttributes(site);

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
            ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
        };

        await _dynamoDb.PutItemAsync(request, ct);
        return site;
    }

    public async Task<IReadOnlyList<Site>> ListByUserAsync(string sub, CancellationToken ct = default)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            FilterExpression = "#status = :status",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "Status"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = $"USER#{sub}" },
                [":sk"] = new AttributeValue { S = "SITE#" },
                [":status"] = new AttributeValue { S = "active" }
            }
        };

        var response = await _dynamoDb.QueryAsync(request, ct);
        return response.Items.Select(FromAttributes).ToList();
    }

    public async Task<Site?> GetByIdAsync(string sub, string siteId, CancellationToken ct = default)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"USER#{sub}" },
                ["SK"] = new AttributeValue { S = $"SITE#{siteId}" }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request, ct);

        if (response.Item.Count == 0)
            return null;

        var site = FromAttributes(response.Item);

        if (site.Sub != sub)
            return null;

        return site;
    }

    private static Dictionary<string, AttributeValue> ToAttributes(Site site)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"USER#{site.Sub}" },
            ["SK"] = new AttributeValue { S = $"SITE#{site.SiteId}" },
            ["SiteId"] = new AttributeValue { S = site.SiteId },
            ["Sub"] = new AttributeValue { S = site.Sub },
            ["SiteName"] = new AttributeValue { S = site.SiteName },
            ["Status"] = new AttributeValue { S = site.Status },
            ["CreatedAt"] = new AttributeValue { S = site.CreatedAt },
            ["UpdatedAt"] = new AttributeValue { S = site.UpdatedAt },
            ["ContentPath"] = new AttributeValue { S = site.ContentPath }
        };

        if (site.SiteUrl != null)
            item["SiteUrl"] = new AttributeValue { S = site.SiteUrl };

        return item;
    }

    public async Task UpdateAsync(string sub, string siteId, string siteUrl, string updatedAt, CancellationToken ct = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"USER#{sub}" },
                ["SK"] = new AttributeValue { S = $"SITE#{siteId}" }
            },
            UpdateExpression = "SET SiteUrl = :url, UpdatedAt = :updated",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":url"] = new AttributeValue { S = siteUrl },
                [":updated"] = new AttributeValue { S = updatedAt }
            }
        };

        await _dynamoDb.UpdateItemAsync(request, ct);
    }

    public async Task DeleteAsync(string sub, string siteId, CancellationToken ct = default)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"USER#{sub}" },
                ["SK"] = new AttributeValue { S = $"SITE#{siteId}" }
            },
            ConditionExpression = "attribute_exists(PK)"
        };

        await _dynamoDb.DeleteItemAsync(request, ct);
    }

    private static Site FromAttributes(Dictionary<string, AttributeValue> item)
    {
        return new Site
        {
            SiteId = item["SiteId"].S,
            Sub = item["Sub"].S,
            SiteName = item["SiteName"].S,
            Status = item["Status"].S,
            CreatedAt = item["CreatedAt"].S,
            UpdatedAt = item["UpdatedAt"].S,
            ContentPath = item["ContentPath"].S,
            SiteUrl = item.TryGetValue("SiteUrl", out var url) ? url.S : null
        };
    }
}
