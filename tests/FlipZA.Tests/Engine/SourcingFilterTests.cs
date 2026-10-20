using FlipZA.Core.Marketplaces;
using FlipZA.Engine.Configurations;
using FlipZA.Engine.Services;

namespace FlipZA.Tests.Engine;

public class SourcingFilterTests
{
    private static readonly TakealotScraperSettings.ImportFilterSettings Settings = new()
    {
        GenericBrandNames = ["Generic", "Unbranded"],
        ExcludedBrands = ["Belkin", "Volkano"],
        ExcludedTitleKeywords = ["bluetooth", "wireless", "smart", "mah", "power bank", "wall charger"]
    };

    private static MarketplaceSearchHit Hit(string title, string? brand) =>
        new("PLID1", title, brand, 299m, 100, 4.5m, true);

    [Theory]
    [InlineData("USB-C to USB-C Braided Cable 2m", null)]
    [InlineData("USB-C to USB-C Braided Cable 2m", "")]
    [InlineData("USB-C to USB-C Braided Cable 2m", "Generic")]
    [InlineData("USB-C to USB-C Braided Cable 2m", "unbranded")]
    [InlineData("Aluminium Laptop Stand", "WAYROCK")] // small private label is allowed
    public void GenericAndPrivateLabelProducts_AreImportFriendly(string title, string? brand) =>
        Assert.True(SourcingFilter.IsImportFriendly(Hit(title, brand), Settings));

    [Theory]
    [InlineData("Braided USB-C Cable", "Belkin")]
    [InlineData("Braided USB-C Cable", "volkano")] // case-insensitive
    public void EstablishedBrands_AreExcluded(string title, string brand) =>
        Assert.False(SourcingFilter.IsImportFriendly(Hit(title, brand), Settings));

    [Theory]
    [InlineData("Bluetooth Selfie Stick Tripod")]
    [InlineData("Wireless Charging Pad")]
    [InlineData("10000mAh Slim Battery Pack")]   // "mah" inside a number still counts
    [InlineData("Smart Plug with Timer")]
    [InlineData("Mini Power Bank")]
    [InlineData("Fast Wall Charger 20W")]
    public void CostlyToImportTitles_AreExcluded(string title) =>
        Assert.False(SourcingFilter.IsImportFriendly(Hit(title, null), Settings));

    [Theory]
    [InlineData("Smartphone Car Holder")]                 // "smart" inside "Smartphone" is fine
    [InlineData("Silicone Strap for Apple Watch 44mm")]   // brand names in titles are allowed
    [InlineData("USB-C Charger Cable 1m")]                // a cable, not a mains charger
    public void SimilarLookingTitles_AreNotExcluded(string title) =>
        Assert.True(SourcingFilter.IsImportFriendly(Hit(title, null), Settings));

    private static readonly TakealotScraperSettings.ImportFilterSettings Fashion = new()
    {
        GenericBrandNames = ["Generic"],
        ExcludedBrands = ["Guess", "Michael Kors"],
        ExcludedTitleKeywords = ["replica", "inspired", "dupe", "1:1", "smart"]
    };

    [Theory]
    [InlineData("Women's Designer Crossbody Bag - Black", null)]
    [InlineData("Men's Automatic Ratchet Belt - Genuine Leather", "IPSTYLE")]
    [InlineData("Men's Gift Set - Wallet, Belt & Notebook in Gift Box", null)]
    public void Fashion_GenericAndPrivateLabel_AreImportFriendly(string title, string? brand) =>
        Assert.True(SourcingFilter.IsImportFriendly(Hit(title, brand), Fashion));

    [Theory]
    [InlineData("Ladies Tote Bag", "Guess")]
    [InlineData("Ladies Tote Bag", "michael kors")]
    [InlineData("LV Inspired Monogram Tote Bag", null)]
    [InlineData("Replica Designer Wallet", null)]
    [InlineData("Men's Smart Watch Fitness Tracker", null)]
    public void Fashion_BrandsCounterfeitSignalsAndSmartwatches_AreExcluded(string title, string? brand) =>
        Assert.False(SourcingFilter.IsImportFriendly(Hit(title, brand), Fashion));

    [Fact]
    public void UnbrandedOnly_RejectsPrivateLabels()
    {
        var settings = new TakealotScraperSettings.ImportFilterSettings
        {
            GenericBrandNames = ["Generic"],
            UnbrandedOnly = true
        };

        Assert.False(SourcingFilter.IsImportFriendly(Hit("Laptop Stand", "WAYROCK"), settings));
        Assert.True(SourcingFilter.IsImportFriendly(Hit("Laptop Stand", "Generic"), settings));
        Assert.True(SourcingFilter.IsImportFriendly(Hit("Laptop Stand", null), settings));
    }
}
