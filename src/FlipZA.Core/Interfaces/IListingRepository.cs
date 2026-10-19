using FlipZA.Core.Marketplaces;

namespace FlipZA.Core.Interfaces;

public interface IListingRepository
{
    /// <summary>
    /// Upserts the product and appends the snapshot to its price history.
    /// An already-tracked product stays tracked.
    /// </summary>
    Task SaveAsync(ScrapedListing listing, CancellationToken cancellationToken);

    /// <summary>Tracked products for a marketplace, least recently refreshed first.</summary>
    Task<IReadOnlyList<string>> GetTrackedAsync(Marketplace marketplace, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the given products as tracked and, if <paramref name="untrackOthers"/> is set, stops
    /// tracking every other product on the marketplace. Returns the ids not yet in the database,
    /// which must be scraped and saved before they can be tracked.
    /// </summary>
    Task<IReadOnlyList<string>> SetTrackedAsync(
        Marketplace marketplace,
        IReadOnlyCollection<string> externalIds,
        bool untrackOthers,
        CancellationToken cancellationToken);

    Task UntrackAsync(Marketplace marketplace, string externalId, CancellationToken cancellationToken);
}
