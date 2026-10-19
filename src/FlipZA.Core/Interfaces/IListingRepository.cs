using FlipZA.Core.Marketplaces;

namespace FlipZA.Core.Interfaces;

public interface IListingRepository
{
    /// <summary>Upserts the product and appends the snapshot to its price history.</summary>
    Task SaveAsync(ScrapedListing listing, CancellationToken cancellationToken);
}
