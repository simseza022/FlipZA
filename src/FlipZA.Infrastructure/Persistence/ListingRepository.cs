using FlipZA.Core.Interfaces;
using FlipZA.Core.Marketplaces;
using Microsoft.EntityFrameworkCore;

namespace FlipZA.Infrastructure.Persistence;

public class ListingRepository(FlipZaDbContext db) : IListingRepository
{
    public async Task SaveAsync(ScrapedListing listing, CancellationToken cancellationToken)
    {
        var scraped = listing.Product;
        var product = await db.MarketplaceProducts.SingleOrDefaultAsync(
            p => p.Marketplace == scraped.Marketplace && p.ExternalId == scraped.ExternalId,
            cancellationToken);

        if (product is null)
        {
            product = scraped;
            db.MarketplaceProducts.Add(product);
        }
        else
        {
            scraped.Id = product.Id;
            scraped.FirstSeenAtUtc = product.FirstSeenAtUtc;
            scraped.IsTracked |= product.IsTracked;
            db.Entry(product).CurrentValues.SetValues(scraped);
        }

        product.Snapshots.Add(listing.Snapshot);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetTrackedAsync(
        Marketplace marketplace, int limit, CancellationToken cancellationToken) =>
        await db.MarketplaceProducts
            .Where(p => p.Marketplace == marketplace && p.IsTracked)
            .OrderBy(p => p.LastSeenAtUtc)
            .Select(p => p.ExternalId)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> SetTrackedAsync(
        Marketplace marketplace,
        IReadOnlyCollection<string> externalIds,
        bool untrackOthers,
        CancellationToken cancellationToken)
    {
        var ids = externalIds.ToList();
        var products = db.MarketplaceProducts.Where(p => p.Marketplace == marketplace);

        if (untrackOthers)
        {
            await products
                .Where(p => p.IsTracked && !ids.Contains(p.ExternalId))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTracked, false), cancellationToken);
        }

        await products
            .Where(p => ids.Contains(p.ExternalId))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTracked, true), cancellationToken);

        var existing = await products
            .Where(p => ids.Contains(p.ExternalId))
            .Select(p => p.ExternalId)
            .ToListAsync(cancellationToken);

        return ids.Except(existing).ToList();
    }

    public async Task UntrackAsync(Marketplace marketplace, string externalId, CancellationToken cancellationToken) =>
        await db.MarketplaceProducts
            .Where(p => p.Marketplace == marketplace && p.ExternalId == externalId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTracked, false), cancellationToken);
}
