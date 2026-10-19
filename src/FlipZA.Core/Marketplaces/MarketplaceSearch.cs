namespace FlipZA.Core.Marketplaces;

/// <summary>A keyword search, optionally limited to one marketplace department.</summary>
public sealed record MarketplaceSearch(string Query, int? DepartmentId = null);

/// <summary>
/// Summary of a product as it appears in search results. Cheap to obtain (dozens per request),
/// so it is used to shortlist products before fetching their full details.
/// </summary>
public sealed record MarketplaceSearchHit(
    string ExternalId,
    string Title,
    decimal? Price,
    int ReviewCount,
    decimal? StarRating,
    bool IsInStock);
