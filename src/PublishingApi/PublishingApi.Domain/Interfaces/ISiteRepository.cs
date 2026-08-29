using PublishingApi.Domain.Models;

namespace PublishingApi.Domain.Interfaces;

public interface ISiteRepository
{
    Task<Site> CreateAsync(Site site, CancellationToken ct = default);
    Task<IReadOnlyList<Site>> ListByUserAsync(string sub, CancellationToken ct = default);
    Task<Site?> GetByIdAsync(string sub, string siteId, CancellationToken ct = default);
    Task UpdateAsync(string sub, string siteId, string siteUrl, string updatedAt, CancellationToken ct = default);
    Task DeleteAsync(string sub, string siteId, CancellationToken ct = default);
}
