namespace FlipZA.Core.Marketplaces;

/// <summary>
/// Result of scraping one product: its catalogue data plus the current snapshot.
/// </summary>
public sealed record ScrapedListing(MarketplaceProduct Product, ListingSnapshot Snapshot);
