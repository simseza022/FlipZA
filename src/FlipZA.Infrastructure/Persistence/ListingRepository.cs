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
            db.Entry(product).CurrentValues.SetValues(scraped);
        }

        product.Snapshots.Add(listing.Snapshot);
        await db.SaveChangesAsync(cancellationToken);
    }
}
