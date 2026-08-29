namespace PublishingApi.Domain.Models;

public record UserContext
{
    public required string Sub { get; init; }
    public required string Email { get; init; }
}
