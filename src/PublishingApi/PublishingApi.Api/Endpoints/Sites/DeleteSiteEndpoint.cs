using System.Security.Claims;
using PublishingApi.Api.Authorization;
using PublishingApi.Api.Extensions;
using PublishingApi.Domain.Interfaces;

namespace PublishingApi.Api.Endpoints.Sites;

public static class DeleteSiteEndpoint
{
    public static void MapDeleteSite(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/sites/{siteId}", async (
            string siteId,
            ClaimsPrincipal principal,
            ISiteRepository repository,
            IStorageService storage,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("DeleteSite");
            var user = principal.ToUserContext();

            var site = await repository.GetByIdAsync(user.Sub, siteId, ct);
            if (site is null)
                return Results.NotFound(new { error = "Site not found" });

            try
            {
                await storage.DeletePrefixAsync(site.ContentPath, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "S3 deletion failed for site {SiteId}; DynamoDB record retained", siteId);
                return Results.Problem(
                    detail: "Failed to delete site content. The site record has been retained and you can retry.",
                    statusCode: 500);
            }

            try
            {
                await repository.DeleteAsync(user.Sub, siteId, ct);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "S3 deleted but DynamoDB delete failed for site {SiteId}; manual cleanup required", siteId);
                return Results.Problem(
                    detail: "Site content was deleted but the site record could not be removed. Manual cleanup required.",
                    statusCode: 500);
            }

            return Results.NoContent();
        })
        .RequireAuthorization(Authorization.ScopePolicies.SitesWrite)
        .WithName("DeleteSite");
    }
}
