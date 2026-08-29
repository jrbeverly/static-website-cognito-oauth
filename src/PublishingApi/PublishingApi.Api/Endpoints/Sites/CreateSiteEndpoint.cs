using System.Security.Claims;
using PublishingApi.Api.Authorization;
using PublishingApi.Api.Extensions;
using PublishingApi.Domain.Interfaces;
using PublishingApi.Domain.Models;
using PublishingApi.Domain.Services;

namespace PublishingApi.Api.Endpoints.Sites;

public static class CreateSiteEndpoint
{
    public record CreateSiteRequest(string SiteName);
    public record CreateSiteResponse(string SiteId, string SiteName, string ContentPath);

    public static void MapCreateSite(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sites", async (
            CreateSiteRequest request,
            ClaimsPrincipal principal,
            ISiteRepository repository,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.SiteName))
                return Results.BadRequest(new { error = "SiteName is required" });

            var user = principal.ToUserContext();
            var siteId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");

            var site = new Site
            {
                SiteId = siteId,
                Sub = user.Sub,
                SiteName = request.SiteName,
                Status = "active",
                CreatedAt = now,
                UpdatedAt = now,
                ContentPath = S3KeyGenerator.GenerateContentPath(user.Sub, siteId),
                SiteUrl = null
            };

            var created = await repository.CreateAsync(site, ct);
            return Results.Created($"/sites/{siteId}", new CreateSiteResponse(
                created.SiteId, created.SiteName, created.ContentPath));
        })
        .RequireAuthorization(Authorization.ScopePolicies.SitesWrite)
        .WithName("CreateSite");
    }
}
