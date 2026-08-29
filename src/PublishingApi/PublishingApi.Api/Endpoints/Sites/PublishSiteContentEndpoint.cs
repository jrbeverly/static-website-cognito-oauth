using System.IO.Compression;
using System.Security.Claims;
using PublishingApi.Api.Authorization;
using PublishingApi.Api.Extensions;
using PublishingApi.Domain.Interfaces;
using PublishingApi.Domain.Services;
using PublishingApi.Infrastructure.Services;

namespace PublishingApi.Api.Endpoints.Sites;

public static class PublishSiteContentEndpoint
{
    public static void MapPublishSiteContent(this IEndpointRouteBuilder app)
    {
        app.MapPut("/sites/{siteId}/content", async (
            string siteId,
            HttpRequest request,
            ClaimsPrincipal principal,
            ISiteRepository repository,
            IStorageService storage,
            ICloudFrontService cloudFront,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("PublishSiteContent");
            var user = principal.ToUserContext();

            var site = await repository.GetByIdAsync(user.Sub, siteId, ct);
            if (site is null)
                return Results.NotFound(new { error = "Site not found" });

            Stream zipStream;
            if (request.HasFormContentType)
            {
                var file = request.Form.Files["file"];
                if (file is null)
                    return Results.BadRequest(new { error = "No file found in form field 'file'" });

                if (file.ContentType != "application/zip" && file.ContentType != "application/octet-stream")
                    return Results.BadRequest(new { error = "File must be a ZIP archive" });

                zipStream = file.OpenReadStream();
            }
            else
            {
                var contentType = request.ContentType ?? "";
                if (contentType != "application/zip" && contentType != "application/octet-stream")
                    return Results.BadRequest(new { error = "Content-Type must be application/zip" });

                zipStream = request.Body;
            }

            var maxSizeMb = configuration.GetValue<int>("MAX_ZIP_SIZE_MB", 50);
            var maxBytes = maxSizeMb * 1024L * 1024L;
            using var ms = new MemoryStream();

            var buffer = new byte[81920];
            long totalRead = 0;
            int read;
            while ((read = await zipStream.ReadAsync(buffer, ct)) > 0)
            {
                totalRead += read;
                if (totalRead > maxBytes)
                    return Results.Problem(statusCode: 413, detail: $"ZIP file exceeds {maxSizeMb} MB limit");
                ms.Write(buffer, 0, read);
            }

            if (totalRead == 0)
                return Results.BadRequest(new { error = "Empty request body" });

            ms.Position = 0;

            ZipArchive archive;
            try
            {
                archive = new ZipArchive(ms, ZipArchiveMode.Read);
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new { error = "Invalid ZIP file" });
            }

            using (archive)
            {
                var maxUncompressedMb = configuration.GetValue<long>("MAX_UNCOMPRESSED_SIZE_MB", 500);
                var validation = ZipValidator.Validate(archive, maxUncompressedMb);
                if (!validation.IsValid)
                    return Results.BadRequest(new { error = validation.Error });

                var entries = validation.Entries!;

                await storage.DeletePrefixAsync(site.ContentPath, ct);

                foreach (var entry in entries)
                {
                    var s3Key = S3KeyGenerator.GenerateFileKey(site.ContentPath, entry.FullName);
                    var contentType = ContentTypeHelper.GetContentType(entry.FullName);
                    await using var entryStream = entry.Open();
                    await storage.UploadObjectAsync(s3Key, entryStream, contentType, ct);
                }
            }

            var cloudFrontDistributionId = configuration["CloudFront:DistributionId"];
            var cloudFrontDomain = configuration["CloudFront:Domain"] ?? "cdn.example.com";

            if (!string.IsNullOrEmpty(cloudFrontDistributionId))
            {
                try
                {
                    await cloudFront.InvalidateAsync(cloudFrontDistributionId, $"/{site.ContentPath}*", ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "CloudFront invalidation failed for site {SiteId}", siteId);
                }
            }

            var now = DateTime.UtcNow.ToString("o");
            var siteUrl = $"https://{cloudFrontDomain}/{user.Sub}/{siteId}/";
            await repository.UpdateAsync(user.Sub, siteId, siteUrl, now, ct);

            return Results.Ok(new { siteUrl });
        })
        .RequireAuthorization(Authorization.ScopePolicies.SitesWrite)
        .DisableAntiforgery()
        .WithName("PublishSiteContent");
    }
}
