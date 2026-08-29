namespace PublishingApi.Domain.Models;

public record Site
{
    public required string SiteId { get; init; }
    public required string Sub { get; init; }
    public required string SiteName { get; init; }
    public required string Status { get; init; }
    public required string CreatedAt { get; init; }
    public required string UpdatedAt { get; init; }
    public required string ContentPath { get; init; }
    public string? SiteUrl { get; init; }
}
