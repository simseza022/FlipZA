namespace FlipZA.Core.Marketplaces;

/// <summary>
/// A non-buy-box offer on the same product, captured with a <see cref="ListingSnapshot"/>.
/// </summary>
public class CompetingOffer
{
    public long Id { get; set; }
    public long SnapshotId { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string? SellerId { get; set; }
    public string? SellerName { get; set; }
    public bool IsMarketplaceOperator { get; set; }
    public bool? SellerVatRegistered { get; set; }
    public decimal Price { get; set; }
    public bool IsInStock { get; set; }
    public string? StockStatus { get; set; }
}
