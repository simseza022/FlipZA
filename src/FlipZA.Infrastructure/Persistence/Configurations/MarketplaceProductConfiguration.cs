using System.Text.Json;
using FlipZA.Core.Marketplaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlipZA.Infrastructure.Persistence.Configurations;

public class MarketplaceProductConfiguration : IEntityTypeConfiguration<MarketplaceProduct>
{
    public void Configure(EntityTypeBuilder<MarketplaceProduct> builder)
    {
        builder.HasIndex(p => new { p.Marketplace, p.ExternalId }).IsUnique();
        builder.HasIndex(p => p.Barcode);
        builder.HasIndex(p => new { p.Marketplace, p.IsTracked, p.LastSeenAtUtc });

        builder.Property(p => p.Marketplace).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.ExternalId).HasMaxLength(64);
        builder.Property(p => p.VariantId).HasMaxLength(64);
        builder.Property(p => p.Title).HasMaxLength(512);
        builder.Property(p => p.Barcode).HasMaxLength(32);

        builder.Property(p => p.Attributes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonSerializerOptions.Default) ?? new())
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>>(
                (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
                v => v.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
                v => new Dictionary<string, string>(v)));

        builder.HasMany(p => p.Snapshots)
            .WithOne(s => s.Product)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
