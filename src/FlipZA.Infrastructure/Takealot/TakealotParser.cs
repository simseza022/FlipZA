using System.Text.Json;
using System.Text.RegularExpressions;
using FlipZA.Core.Marketplaces;
using FlipZA.Infrastructure.Json;

namespace FlipZA.Infrastructure.Takealot;

public sealed record TakealotSearchPage(IReadOnlyList<string> ProductIds, string? NextPageToken, int TotalFound);

/// <summary>
/// Maps Takealot's public REST API responses onto FlipZA's marketplace entities.
/// </summary>
public static partial class TakealotParser
{
    private const string MarketplaceOperatorName = "Takealot";
    private const string ImageSize = "zoom";

    public static TakealotSearchPage ParseSearch(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var products = doc.RootElement.Prop("sections", "products");

        var ids = products.Prop("results").Items()
            .Select(r => r.Prop("product_views", "core", "id").AsString())
            .Where(id => id is not null)
            .Select(id => $"PLID{id}")
            .Distinct()
            .ToList();

        var next = products.Prop("paging", "next_is_after").AsString();
        var total = products.Prop("paging", "total_num_found").AsInt() ?? ids.Count;

        return new TakealotSearchPage(ids, string.IsNullOrEmpty(next) ? null : next, total);
    }

    public static ScrapedListing ParseProductDetails(string json, DateTime capturedAtUtc)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var attributes = ParseAttributes(root);
        var breadcrumbs = root.Prop("breadcrumbs", "items").Items().ToList();

        var product = new MarketplaceProduct
        {
            Marketplace = Marketplace.Takealot,
            ExternalId = root.Prop("meta", "identifier").AsString()
                         ?? $"PLID{root.Prop("buybox", "plid").AsString()}",
            VariantId = root.Prop("buybox", "tsin").AsString(),
            Title = root.Prop("title").AsString() ?? root.Prop("core", "title").AsString() ?? string.Empty,
            Subtitle = root.Prop("core", "subtitle").AsString(),
            Brand = root.Prop("core", "brand").AsString(),
            Barcode = attributes.GetValueOrDefault("barcode") ?? root.Prop("flixmedia", "ean").AsString(),
            ModelNumber = attributes.GetValueOrDefault("model_number"),
            Department = breadcrumbs.FirstOrDefault(b => b.Prop("type").AsString() == "department").Prop("name").AsString(),
            CategoryPath = breadcrumbs.Count == 0
                ? null
                : string.Join(" > ", breadcrumbs.Select(b => b.Prop("name").AsString())),
            LeafCategoryId = breadcrumbs.Count == 0 ? null : breadcrumbs[^1].Prop("id").AsInt(),
            Url = root.Prop("desktop_href").AsString() ?? string.Empty,
            ImageUrl = root.Prop("gallery", "images").Items()
                .Select(i => ((JsonElement?)i).AsString())
                .FirstOrDefault()?.Replace("{size}", ImageSize),
            Dimensions = FirstAttribute(attributes, "dimensions"),
            Weight = FirstAttribute(attributes, "weight"),
            Warranty = attributes.GetValueOrDefault("warranty"),
            Attributes = attributes,
            FirstSeenAtUtc = capturedAtUtc,
            LastSeenAtUtc = capturedAtUtc,
            LastRawPayload = json
        };

        return new ScrapedListing(product, ParseSnapshot(root, capturedAtUtc));
    }

    private static ListingSnapshot ParseSnapshot(JsonElement root, DateTime capturedAtUtc)
    {
        var buyBox = root.Prop("buybox", "items").Items()
            .FirstOrDefault(i => ((JsonElement?)i).Prop("is_selected").AsBool() == true);
        JsonElement? selected = buyBox.ValueKind == JsonValueKind.Object ? buyBox : null;

        var eventProduct = root.Prop("event_data", "documents", "product");
        var stock = selected.Prop("stock_availability");
        var seller = root.Prop("seller_detail");
        var isMarketplaceSeller = eventProduct.Prop("market_place_listing").AsBool() ?? seller is not null;

        var offers = ParseOffers(root);

        return new ListingSnapshot
        {
            CapturedAtUtc = capturedAtUtc,
            Price = selected.Prop("price").AsDecimal() ?? eventProduct.Prop("purchase_price").AsDecimal(),
            ListPrice = selected.Prop("listing_price").AsDecimal() ?? eventProduct.Prop("original_price").AsDecimal(),
            IsInStock = eventProduct.Prop("in_stock").AsBool() ?? false,
            IsLeadTime = stock.Prop("is_leadtime").AsBool() ?? false,
            StockStatus = stock.Prop("status").AsString(),
            DistributionCentres = JoinOrNull(stock.Prop("distribution_centres").Items()
                .Select(dc => ((JsonElement?)dc).Prop("text").AsString())),
            IsMarketplaceSeller = isMarketplaceSeller,
            BuyBoxSellerId = isMarketplaceSeller ? seller.Prop("seller_id").AsString() : null,
            BuyBoxSellerName = isMarketplaceSeller ? seller.Prop("display_name").AsString() : MarketplaceOperatorName,
            BuyBoxSellerVatRegistered = isMarketplaceSeller ? seller.Prop("seller_vat_status", "enabled").AsBool() : true,
            BuyBoxSellerRating = seller.Prop("seller_reviews", "average_star_rating").AsDecimal(),
            StarRating = root.Prop("reviews", "star_rating").AsDecimal() ?? root.Prop("core", "star_rating").AsDecimal(),
            ReviewCount = root.Prop("reviews", "count").AsInt() ?? root.Prop("core", "reviews").AsInt() ?? 0,
            OtherOffersCount = offers.Count,
            LowestOtherOfferPrice = offers.Count == 0 ? null : offers.Min(o => o.Price),
            PromotionName = eventProduct.Prop("promotions").Items()
                .Select(p => ((JsonElement?)p).Prop("promo_name").AsString())
                .FirstOrDefault(n => n is not null),
            Offers = offers
        };
    }

    private static List<CompetingOffer> ParseOffers(JsonElement root)
    {
        var offers = new List<CompetingOffer>();

        foreach (var condition in root.Prop("other_offers", "conditions").Items())
        {
            var conditionName = ((JsonElement?)condition).Prop("condition").AsString() ?? "Unknown";

            foreach (var item in ((JsonElement?)condition).Prop("items").Items())
            {
                JsonElement? offer = item;
                var price = offer.Prop("price").AsDecimal();
                if (price is null)
                    continue;

                var isOperator = offer.Prop("is_takealot").AsBool() ?? false;
                offers.Add(new CompetingOffer
                {
                    Condition = conditionName,
                    SellerId = offer.Prop("seller", "seller_id").AsString(),
                    SellerName = isOperator ? MarketplaceOperatorName : offer.Prop("seller", "display_name").AsString()?.Trim(),
                    IsMarketplaceOperator = isOperator,
                    SellerVatRegistered = isOperator ? true : offer.Prop("seller", "seller_vat_status", "enabled").AsBool(),
                    Price = price.Value,
                    IsInStock = offer.Prop("stock_availability", "is_in_stock").AsBool() ?? false,
                    StockStatus = offer.Prop("stock_availability", "status").AsString()
                });
            }
        }

        return offers;
    }

    private static Dictionary<string, string> ParseAttributes(JsonElement root)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in root.Prop("product_information", "items").Items())
        {
            JsonElement? element = item;
            var key = element.Prop("item_type").AsString();
            var value = element.Prop("displayable_text").AsString();
            if (key is null || value is null)
                continue;

            attributes[key] = CleanMarkdown(value);
        }

        return attributes;
    }

    private static string? FirstAttribute(Dictionary<string, string> attributes, string keyFragment) =>
        attributes.FirstOrDefault(a => a.Key.Contains(keyFragment, StringComparison.OrdinalIgnoreCase)).Value;

    private static string? JoinOrNull(IEnumerable<string?> values)
    {
        var joined = string.Join(",", values.Where(v => !string.IsNullOrWhiteSpace(v)));
        return joined.Length == 0 ? null : joined;
    }

    /// <summary>Reduces Takealot's markdown snippets to plain text: links to their label, escapes removed, lines joined.</summary>
    private static string CleanMarkdown(string markdown)
    {
        var text = MarkdownLink().Replace(markdown, "$1");
        text = MarkdownEscape().Replace(text, "$1");
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join("; ", lines);
    }

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"\\(.)")]
    private static partial Regex MarkdownEscape();
}
