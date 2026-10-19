namespace FlipZA.Core.Marketplaces;

/// <summary>
/// Slow-changing catalogue data for a product listed on a marketplace.
/// Price and stock live on <see cref="ListingSnapshot"/> so they can be tracked over time.
/// </summary>
public class MarketplaceProduct
{
    public int Id { get; set; }
    public Marketplace Marketplace { get; set; }

    /// <summary>The marketplace's own identifier, e.g. "PLID98945485" on Takealot.</summary>
    public string ExternalId { get; set; } = string.Empty;

    public string? VariantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Brand { get; set; }

    /// <summary>EAN/UPC barcode, the primary key for matching against supplier offers.</summary>
    public string? Barcode { get; set; }

    public string? ModelNumber { get; set; }
    public string? Department { get; set; }
    public string? CategoryPath { get; set; }

    /// <summary>Most specific category id; marketplace fees are set per category.</summary>
    public int? LeafCategoryId { get; set; }

    public string Url { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Dimensions { get; set; }
    public string? Weight { get; set; }
    public string? Warranty { get; set; }

    /// <summary>All remaining product attributes, keyed by the marketplace's attribute type.</summary>
    public Dictionary<string, string> Attributes { get; set; } = new();

    /// <summary>Tracked products are re-scraped on every refresh run to build price history.</summary>
    public bool IsTracked { get; set; }

    /// <summary>The sourcing channel the product was last chosen for; null if never tracked.</summary>
    public SourcingChannel? SourcingChannel { get; set; }

    public DateTime FirstSeenAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }

    /// <summary>Latest raw API payload, kept so data can be re-parsed if the parser changes.</summary>
    public string? LastRawPayload { get; set; }

    public List<ListingSnapshot> Snapshots { get; set; } = new();
}
