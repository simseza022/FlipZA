using FlipZA.Core.Marketplaces;

namespace FlipZA.Core.Interfaces;

public interface IMarketplaceSource
{
    Marketplace Marketplace { get; }

    /// <summary>Runs a keyword search and returns the external ids of matching products.</summary>
    Task<IReadOnlyList<string>> SearchAsync(string query, int maxPages, CancellationToken cancellationToken);

    /// <summary>Fetches full details for one product, or null if it no longer exists.</summary>
    Task<ScrapedListing?> GetListingAsync(string externalId, CancellationToken cancellationToken);
}
