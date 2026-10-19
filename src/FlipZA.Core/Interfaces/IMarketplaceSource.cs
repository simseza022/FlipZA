using FlipZA.Core.Marketplaces;

namespace FlipZA.Core.Interfaces;

public interface IMarketplaceSource
{
    Marketplace Marketplace { get; }

    /// <summary>Runs a search and returns summaries of the matching products, in marketplace ranking order.</summary>
    Task<IReadOnlyList<MarketplaceSearchHit>> SearchAsync(MarketplaceSearch search, int maxPages, CancellationToken cancellationToken);

    /// <summary>Fetches full details for one product, or null if it no longer exists.</summary>
    Task<ScrapedListing?> GetListingAsync(string externalId, CancellationToken cancellationToken);
}
