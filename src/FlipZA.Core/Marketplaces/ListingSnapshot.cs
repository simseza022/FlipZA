namespace FlipZA.Core.Marketplaces;

/// <summary>
/// Point-in-time view of a listing's buy box, stock and competition.
/// </summary>
public class ListingSnapshot
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public MarketplaceProduct? Product { get; set; }
    public DateTime CapturedAtUtc { get; set; }

    /// <summary>Current buy box selling price; null when the product can't be bought.</summary>
    public decimal? Price { get; set; }

    /// <summary>"Was" price shown struck through, if any.</summary>
    public decimal? ListPrice { get; set; }

    public bool IsInStock { get; set; }

    /// <summary>True when the buy box offer ships from the seller rather than marketplace warehouses.</summary>
    public bool IsLeadTime { get; set; }

    public string? StockStatus { get; set; }

    /// <summary>Comma-separated warehouse codes the buy box offer ships from, e.g. "CPT,JHB".</summary>
    public string? DistributionCentres { get; set; }

    /// <summary>True when a third-party seller (not the marketplace itself) holds the buy box.</summary>
    public bool IsMarketplaceSeller { get; set; }

    public string? BuyBoxSellerId { get; set; }
    public string? BuyBoxSellerName { get; set; }
    public bool? BuyBoxSellerVatRegistered { get; set; }
    public decimal? BuyBoxSellerRating { get; set; }

    public decimal? StarRating { get; set; }

    /// <summary>Review count over time is a proxy for sales velocity.</summary>
    public int ReviewCount { get; set; }

    public int OtherOffersCount { get; set; }
    public decimal? LowestOtherOfferPrice { get; set; }
    public string? PromotionName { get; set; }

    public List<CompetingOffer> Offers { get; set; } = new();
}
