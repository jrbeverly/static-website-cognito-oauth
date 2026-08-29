using System.Security.Claims;
using PublishingApi.Api.Authorization;
using PublishingApi.Api.Extensions;
using PublishingApi.Domain.Interfaces;

namespace PublishingApi.Api.Endpoints.Sites;

public static class GetSiteEndpoint
{
    public record GetSiteResponse(
        string SiteId,
        string SiteName,
        string Status,
        string? SiteUrl,
        string CreatedAt,
        string UpdatedAt,
        string ContentPath);

    public static void MapGetSite(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sites/{siteId}", async (
            string siteId,
            ClaimsPrincipal principal,
            ISiteRepository repository,
            CancellationToken ct) =>
        {
            var user = principal.ToUserContext();
            var site = await repository.GetByIdAsync(user.Sub, siteId, ct);

            if (site is null)
                return Results.NotFound(new { error = "Site not found" });

            return Results.Ok(new GetSiteResponse(
                site.SiteId, site.SiteName, site.Status,
                site.SiteUrl, site.CreatedAt, site.UpdatedAt,
                site.ContentPath));
        })
        .RequireAuthorization(Authorization.ScopePolicies.SitesRead)
        .WithName("GetSite");
    }
}
