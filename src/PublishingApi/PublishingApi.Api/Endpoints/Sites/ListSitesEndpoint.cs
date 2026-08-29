using System.Security.Claims;
using PublishingApi.Api.Authorization;
using PublishingApi.Api.Extensions;
using PublishingApi.Domain.Interfaces;

namespace PublishingApi.Api.Endpoints.Sites;

public static class ListSitesEndpoint
{
    public record ListSitesResponse(
        string SiteId,
        string SiteName,
        string Status,
        string? SiteUrl,
        string CreatedAt,
        string UpdatedAt);

    public static void MapListSites(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sites", async (
            ClaimsPrincipal principal,
            ISiteRepository repository,
            CancellationToken ct) =>
        {
            var user = principal.ToUserContext();
            var sites = await repository.ListByUserAsync(user.Sub, ct);

            var response = sites.Select(s => new ListSitesResponse(
                s.SiteId, s.SiteName, s.Status, s.SiteUrl, s.CreatedAt, s.UpdatedAt));

            return Results.Ok(response);
        })
        .RequireAuthorization(Authorization.ScopePolicies.SitesRead)
        .WithName("ListSites");
    }
}
