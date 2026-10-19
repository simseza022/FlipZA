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

    public void Dispose() => _connection.Dispose();
}
