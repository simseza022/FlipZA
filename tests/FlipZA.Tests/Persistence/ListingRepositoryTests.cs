using FlipZA.Core.Marketplaces;
using FlipZA.Infrastructure.Persistence;
using FlipZA.Infrastructure.Takealot;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlipZA.Tests.Persistence;

public sealed class ListingRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<FlipZaDbContext> _options;

    public ListingRepositoryTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<FlipZaDbContext>().UseSqlite(_connection).Options;
        using var db = new FlipZaDbContext(_options);
        db.Database.Migrate();
    }

    [Fact]
    public async Task SaveAsync_SameProductTwice_UpsertsProductAndAppendsSnapshots()
    {
        var json = Fixture.Read("Takealot", "product-details-retail.json");
        var first = new DateTime(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);
        var second = first.AddHours(1);

        await using (var db = new FlipZaDbContext(_options))
            await new ListingRepository(db).SaveAsync(TakealotParser.ParseProductDetails(json, first), CancellationToken.None);

        var rescraped = TakealotParser.ParseProductDetails(json, second);
        rescraped.Snapshot.Price = 999m;
        await using (var db = new FlipZaDbContext(_options))
            await new ListingRepository(db).SaveAsync(rescraped, CancellationToken.None);

        await using var verify = new FlipZaDbContext(_options);
        var product = await verify.MarketplaceProducts
            .Include(p => p.Snapshots).ThenInclude(s => s.Offers)
            .SingleAsync();

        Assert.Equal(first, product.FirstSeenAtUtc);
        Assert.Equal(second, product.LastSeenAtUtc);
        Assert.Equal("Russell Hobbs", product.Attributes["brand"]);
        Assert.Equal([1099m, 999m], product.Snapshots.OrderBy(s => s.CapturedAtUtc).Select(s => s.Price));
        Assert.All(product.Snapshots, s => Assert.Equal(6, s.Offers.Count));

        var cheapCount = await verify.ListingSnapshots.CountAsync(s => s.Price < 1000m);
        Assert.Equal(1, cheapCount);
    }

    [Fact]
    public async Task SetTrackedAsync_TracksKnownProducts_ReturnsUnknown_AndPrunesOthers()
    {
        await SaveFixtureAsync("product-details-retail.json", track: true);        // PLID98945485
        await SaveFixtureAsync("product-details-marketplace.json", track: false);  // PLID96021549

        await using (var db = new FlipZaDbContext(_options))
        {
            var unknown = await new ListingRepository(db).SetTrackedAsync(
                Marketplace.Takealot,
                [new("PLID96021549", SourcingChannel.Import), new("PLID11111111", SourcingChannel.Local)],
                untrackOthers: true, CancellationToken.None);

            Assert.Equal([new TrackingTarget("PLID11111111", SourcingChannel.Local)], unknown);
        }

        await using var verify = new FlipZaDbContext(_options);
        var tracked = await new ListingRepository(verify).GetTrackedAsync(Marketplace.Takealot, 10, CancellationToken.None);
        Assert.Equal(["PLID96021549"], tracked);

        var channel = await verify.MarketplaceProducts
            .Where(p => p.ExternalId == "PLID96021549")
            .Select(p => p.SourcingChannel)
            .SingleAsync();
        Assert.Equal(SourcingChannel.Import, channel);
    }

    [Fact]
    public async Task SaveAsync_Refresh_KeepsSourcingChannel()
    {
        await SaveFixtureAsync("product-details-retail.json", track: true, channel: SourcingChannel.Import);
        await SaveFixtureAsync("product-details-retail.json", track: false); // a refresh has no channel

        await using var verify = new FlipZaDbContext(_options);
        var product = await verify.MarketplaceProducts.SingleAsync();
        Assert.Equal(SourcingChannel.Import, product.SourcingChannel);
    }

    [Fact]
    public async Task SetTrackedAsync_WithoutPruning_KeepsExistingTrackedProducts()
    {
        await SaveFixtureAsync("product-details-retail.json", track: true);
        await SaveFixtureAsync("product-details-marketplace.json", track: false);

        await using (var db = new FlipZaDbContext(_options))
            await new ListingRepository(db).SetTrackedAsync(
                Marketplace.Takealot, [new("PLID96021549", SourcingChannel.Local)], untrackOthers: false, CancellationToken.None);

        await using var verify = new FlipZaDbContext(_options);
        var tracked = await new ListingRepository(verify).GetTrackedAsync(Marketplace.Takealot, 10, CancellationToken.None);
        Assert.Equal(2, tracked.Count);
    }

    [Fact]
    public async Task SaveAsync_RefreshOfTrackedProduct_StaysTracked_AndUntrackAsyncStopsIt()
    {
        await SaveFixtureAsync("product-details-retail.json", track: true);
        await SaveFixtureAsync("product-details-retail.json", track: false); // a refresh doesn't set the flag

        await using (var db = new FlipZaDbContext(_options))
        {
            var repository = new ListingRepository(db);
            Assert.Equal(["PLID98945485"], await repository.GetTrackedAsync(Marketplace.Takealot, 10, CancellationToken.None));

            await repository.UntrackAsync(Marketplace.Takealot, "PLID98945485", CancellationToken.None);
        }

        await using var verify = new FlipZaDbContext(_options);
        Assert.Empty(await new ListingRepository(verify).GetTrackedAsync(Marketplace.Takealot, 10, CancellationToken.None));
    }

    [Fact]
    public async Task GetTrackedAsync_ReturnsStalestFirst_UpToLimit()
    {
        await SaveFixtureAsync("product-details-retail.json", track: true, capturedAt: new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc));
        await SaveFixtureAsync("product-details-marketplace.json", track: true, capturedAt: new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc));

        await using var db = new FlipZaDbContext(_options);
        var tracked = await new ListingRepository(db).GetTrackedAsync(Marketplace.Takealot, 1, CancellationToken.None);

        Assert.Equal(["PLID96021549"], tracked);
    }

    private async Task SaveFixtureAsync(
        string fixture, bool track, DateTime? capturedAt = null, SourcingChannel? channel = null)
    {
        var listing = TakealotParser.ParseProductDetails(
            Fixture.Read("Takealot", fixture), capturedAt ?? DateTime.UtcNow);
        listing.Product.IsTracked = track;
        listing.Product.SourcingChannel = channel;

        await using var db = new FlipZaDbContext(_options);
        await new ListingRepository(db).SaveAsync(listing, CancellationToken.None);
    }

    public void Dispose() => _connection.Dispose();
}
