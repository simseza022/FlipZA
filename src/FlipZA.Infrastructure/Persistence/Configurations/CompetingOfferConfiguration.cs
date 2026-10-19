using FlipZA.Core.Marketplaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlipZA.Infrastructure.Persistence.Configurations;

public class CompetingOfferConfiguration : IEntityTypeConfiguration<CompetingOffer>
{
    public void Configure(EntityTypeBuilder<CompetingOffer> builder)
    {
        builder.Property(o => o.Condition).HasMaxLength(64);
        builder.Property(o => o.SellerId).HasMaxLength(64);
        builder.Property(o => o.SellerName).HasMaxLength(256);
        builder.Property(o => o.StockStatus).HasMaxLength(128);
    }
}
