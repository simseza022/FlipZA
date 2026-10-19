using FlipZA.Core.Marketplaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlipZA.Infrastructure.Persistence.Configurations;

public class ListingSnapshotConfiguration : IEntityTypeConfiguration<ListingSnapshot>
{
    public void Configure(EntityTypeBuilder<ListingSnapshot> builder)
    {
        builder.HasIndex(s => new { s.ProductId, s.CapturedAtUtc });

        builder.Property(s => s.StockStatus).HasMaxLength(128);
        builder.Property(s => s.DistributionCentres).HasMaxLength(64);
        builder.Property(s => s.BuyBoxSellerId).HasMaxLength(64);
        builder.Property(s => s.BuyBoxSellerName).HasMaxLength(256);
        builder.Property(s => s.PromotionName).HasMaxLength(256);

        builder.HasMany(s => s.Offers)
            .WithOne()
            .HasForeignKey(o => o.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
