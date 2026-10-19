using FlipZA.Core.Deals;
using FlipZA.Engine.Configurations;
using FlipZA.Engine.Services;
using FlipZA.Infrastructure.Takealot;
using Microsoft.Extensions.Options;

namespace FlipZA.Tests.Engine;

public class ListingPricerTests
{
    [Fact]
    public void Apply_StampsSnapshotWithNetProceedsAndMaxBuyPrice()
    {
        var listing = TakealotParser.ParseProductDetails(
            Fixture.Read("Takealot", "product-details-retail.json"), DateTime.UtcNow); // R1,099 air fryer
        var settings = new DealSettings { Reseller = new ResellerProfile { IsVatRegistered = false } };

        new ListingPricer(Options.Create(settings)).Apply(listing);

        // 1099 - (164.85 + 38) x 1.15 = 865.7225
        Assert.Equal(865.72m, listing.Snapshot.EstimatedNetProceeds);
        // min(865.7225 - 80, 865.7225 / 1.25) = 692.578
        Assert.Equal(692.57m, listing.Snapshot.MaxBuyPrice);
    }

    [Fact]
    public void Apply_LeavesFieldsEmpty_WhenListingHasNoPrice()
    {
        var listing = TakealotParser.ParseProductDetails("""{"title":"Unavailable","buybox":{"plid":1}}""", DateTime.UtcNow);

        new ListingPricer(Options.Create(new DealSettings())).Apply(listing);

        Assert.Null(listing.Snapshot.EstimatedNetProceeds);
        Assert.Null(listing.Snapshot.MaxBuyPrice);
    }
}
