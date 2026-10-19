using FlipZA.Core.Marketplaces;
using Microsoft.EntityFrameworkCore;

namespace FlipZA.Infrastructure.Persistence;

public class FlipZaDbContext(DbContextOptions<FlipZaDbContext> options) : DbContext(options)
{
    public DbSet<MarketplaceProduct> MarketplaceProducts => Set<MarketplaceProduct>();
    public DbSet<ListingSnapshot> ListingSnapshots => Set<ListingSnapshot>();
    public DbSet<CompetingOffer> CompetingOffers => Set<CompetingOffer>();

    // SQLite stores decimal as TEXT, which breaks price comparisons and ORDER BY in queries.
    // REAL is exact enough for rand amounts, and the domain keeps decimal arithmetic.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<decimal>().HaveConversion<double>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlipZaDbContext).Assembly);
}
