using FlipZA.Core.Marketplaces;
using FlipZA.Engine.Jobs;

namespace FlipZA.Tests.Engine;

public class TakealotDiscoveryJobTests
{
    private static MarketplaceSearchHit Hit(string id, int reviews) =>
        new(id, id, null, 299m, reviews, 4.5m, true);

    [Fact]
    public void PickTrackedSet_GivesEachSearchAShare_BeforeRankingByReviews()
    {
        var audio = new List<MarketplaceSearchHit> { Hit("A1", 5000), Hit("A2", 4000), Hit("A3", 3000) };
        var cables = new List<MarketplaceSearchHit> { Hit("C1", 50), Hit("C2", 40) };

        var picked = TakealotDiscoveryJob.PickTrackedSet([audio, cables], maxTracked: 4);

        // Two slots each; pure review ranking would have taken A1, A2, A3 and C1.
        Assert.Equal(["A1", "A2", "C1", "C2"], picked);
    }

    [Fact]
    public void PickTrackedSet_FillsUnusedShares_WithMostReviewedLeftovers()
    {
        var audio = new List<MarketplaceSearchHit> { Hit("A1", 5000), Hit("A2", 4000), Hit("A3", 3000) };
        var cables = new List<MarketplaceSearchHit> { Hit("C1", 50) };

        var picked = TakealotDiscoveryJob.PickTrackedSet([audio, cables], maxTracked: 4);

        Assert.Equal(["A1", "A2", "C1", "A3"], picked);
    }

    [Fact]
    public void PickTrackedSet_DeduplicatesProductsFoundByMultipleSearches()
    {
        var earbuds = new List<MarketplaceSearchHit> { Hit("X", 900), Hit("E1", 100) };
        var earphones = new List<MarketplaceSearchHit> { Hit("X", 900), Hit("P1", 80) };

        var picked = TakealotDiscoveryJob.PickTrackedSet([earbuds, earphones], maxTracked: 10);

        Assert.Equal(["X", "E1", "P1"], picked);
    }
}
