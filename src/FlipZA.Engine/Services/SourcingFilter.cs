using System.Text.RegularExpressions;
using FlipZA.Core.Marketplaces;
using FlipZA.Engine.Configurations;

namespace FlipZA.Engine.Services;

/// <summary>
/// Decides whether a search hit suits the import channel: a generic or small private-label product
/// (not an established brand, which risks counterfeits) that is simple to import (no radio,
/// battery or mains-charger keywords, which bring certification and shipping costs).
/// </summary>
public static class SourcingFilter
{
    public static bool IsImportFriendly(MarketplaceSearchHit hit, TakealotScraperSettings.ImportFilterSettings settings)
    {
        var brand = hit.Brand?.Trim();
        var unbranded = string.IsNullOrEmpty(brand)
                        || settings.GenericBrandNames.Contains(brand, StringComparer.OrdinalIgnoreCase);

        if (!unbranded)
        {
            if (settings.UnbrandedOnly)
                return false;
            if (settings.ExcludedBrands.Contains(brand!, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        // Brands are only checked in the brand field: generic titles often say "for Apple" or "Samsung compatible".
        return !settings.ExcludedTitleKeywords.Any(keyword => ContainsWord(hit.Title, keyword));
    }

    /// <summary>
    /// Case-insensitive match of a keyword not embedded in a longer word: "smart" matches
    /// "Smart Plug" but not "Smartphone", while "mah" still matches "10000mAh".
    /// </summary>
    internal static bool ContainsWord(string text, string keyword) =>
        !string.IsNullOrWhiteSpace(keyword)
        && Regex.IsMatch(text, $"(?<![a-z]){Regex.Escape(keyword.Trim())}(?![a-z])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
