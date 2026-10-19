using FlipZA.Core.Deals;
using FlipZA.Core.Marketplaces;
using FlipZA.Engine.Configurations;
using Microsoft.Extensions.Options;

namespace FlipZA.Engine.Services;

/// <summary>
/// Stamps each scraped snapshot with its deal economics under the configured fees, reseller
/// profile and gate, so price history also shows what each listing was worth to resell.
/// </summary>
public class ListingPricer(IOptions<DealSettings> options)
{
    public void Apply(ScrapedListing listing)
    {
        var (product, snapshot) = listing;
        if (snapshot.Price is not { } price)
            return;

        var deals = options.Value;
        snapshot.EstimatedNetProceeds = DealCalculator
            .CalculateProceeds(price, product.Department, product.LeafCategoryId, deals.Fees, deals.Reseller)
            .NetProceeds;
        snapshot.MaxBuyPrice = DealCalculator.MaxBuyPrice(
            price, product.Department, product.LeafCategoryId, deals.Fees, deals.Reseller, deals.Gate);
    }
}
