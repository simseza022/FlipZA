using FlipZA.Core.Marketplaces;
using FlipZA.Infrastructure.Takealot;

namespace FlipZA.Tests.Takealot;

public class TakealotParserTests
{
    private static readonly DateTime CapturedAt = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ParseSearch_ReturnsHitsAndNextPageToken()
    {
        var page = TakealotParser.ParseSearch(Fixture.Read("Takealot", "search.json"));

        Assert.Equal(3, page.Hits.Count);
        Assert.All(page.Hits, h => Assert.StartsWith("PLID", h.ExternalId));
        Assert.NotNull(page.NextPageToken);
        Assert.True(page.TotalFound > page.Hits.Count);

        var first = page.Hits[0];
        Assert.Equal("PLID98945485", first.ExternalId);
        Assert.Equal("Russell Hobbs Digital 9L Air Fryer - RHAF09S", first.Title);
        Assert.Equal(1099m, first.Price);
        Assert.Equal(2148, first.ReviewCount);
        Assert.Equal(4.8m, first.StarRating);
        Assert.Equal("Russell Hobbs", first.Brand);
        Assert.True(first.IsInStock);
    }

    [Fact]
    public void ParseProductDetails_RetailListing_MapsCatalogueData()
    {
        var (product, _) = TakealotParser.ParseProductDetails(
            Fixture.Read("Takealot", "product-details-retail.json"), CapturedAt);

        Assert.Equal(Marketplace.Takealot, product.Marketplace);
        Assert.Equal("PLID98945485", product.ExternalId);
        Assert.Equal("100366962", product.VariantId);
        Assert.Equal("Russell Hobbs Digital 9L Air Fryer - RHAF09S", product.Title);
        Assert.Equal("Russell Hobbs", product.Brand);
        Assert.Equal("6001498215642", product.Barcode);
        Assert.Equal("RHAF09S", product.ModelNumber);
        Assert.Equal("Home & Kitchen", product.Department);
        Assert.Equal("Home & Kitchen > Small Kitchen Appliances > Cooking Appliances > Air Fryers", product.CategoryPath);
        Assert.Equal(25752, product.LeafCategoryId);
        Assert.Equal("39.2 x 45.7 x 36.5 cm", product.Dimensions);
        Assert.Equal("Limited (12 months)", product.Warranty);
        Assert.Equal("Russell Hobbs", product.Attributes["brand"]);
        Assert.Contains("; ", product.Attributes["whats_in_the_box"]);
        Assert.EndsWith("/s-zoom.file", product.ImageUrl);
        Assert.StartsWith("https://www.takealot.com/", product.Url);
        Assert.NotNull(product.LastRawPayload);
    }

    [Fact]
    public void ParseProductDetails_RetailListing_MapsBuyBoxAndCompetition()
    {
        var (_, snapshot) = TakealotParser.ParseProductDetails(
            Fixture.Read("Takealot", "product-details-retail.json"), CapturedAt);

        Assert.Equal(CapturedAt, snapshot.CapturedAtUtc);
        Assert.Equal(1099m, snapshot.Price);
        Assert.Equal(1499m, snapshot.ListPrice);
        Assert.True(snapshot.IsInStock);
        Assert.False(snapshot.IsLeadTime);
        Assert.Equal("CPT,DBN,JHB", snapshot.DistributionCentres);

        Assert.False(snapshot.IsMarketplaceSeller);
        Assert.Equal("Takealot", snapshot.BuyBoxSellerName);
        Assert.Null(snapshot.BuyBoxSellerId);

        Assert.Equal(4.8m, snapshot.StarRating);
        Assert.Equal(2148, snapshot.ReviewCount);
        Assert.Equal("Pay Day Deals", snapshot.PromotionName);

        Assert.Equal(6, snapshot.OtherOffersCount);
        Assert.Equal(1190m, snapshot.LowestOtherOfferPrice);
        var cheapest = snapshot.Offers.MinBy(o => o.Price)!;
        Assert.Equal("MICAMA CONSULTING", cheapest.SellerName);
        Assert.Equal("29858897", cheapest.SellerId);
        Assert.Equal("New", cheapest.Condition);
        Assert.False(cheapest.SellerVatRegistered);
        Assert.Contains(snapshot.Offers, o => o.SellerName == "ANGLO  INVESTMENT".Trim());
    }

    [Fact]
    public void ParseProductDetails_MarketplaceListing_MapsThirdPartySeller()
    {
        var (product, snapshot) = TakealotParser.ParseProductDetails(
            Fixture.Read("Takealot", "product-details-marketplace.json"), CapturedAt);

        Assert.Equal("PLID96021549", product.ExternalId);
        Assert.Equal("70736010102", product.Barcode);

        Assert.True(snapshot.IsMarketplaceSeller);
        Assert.Equal("29840247", snapshot.BuyBoxSellerId);
        Assert.Equal("Daphni SA", snapshot.BuyBoxSellerName);
        Assert.True(snapshot.BuyBoxSellerVatRegistered);
        Assert.Equal(4.3m, snapshot.BuyBoxSellerRating);
        Assert.Equal(439m, snapshot.Price);
        Assert.Equal(0, snapshot.OtherOffersCount);
        Assert.Null(snapshot.LowestOtherOfferPrice);
    }

    [Fact]
    public void ParseProductDetails_MissingSections_DoesNotThrow()
    {
        var (product, snapshot) = TakealotParser.ParseProductDetails(
            """{"title":"Bare","buybox":{"plid":123}}""", CapturedAt);

        Assert.Equal("PLID123", product.ExternalId);
        Assert.Equal("Bare", product.Title);
        Assert.Null(product.Barcode);
        Assert.Null(snapshot.Price);
        Assert.False(snapshot.IsInStock);
        Assert.Empty(snapshot.Offers);
    }
}
