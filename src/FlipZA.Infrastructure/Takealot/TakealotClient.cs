using System.Net;
using FlipZA.Core.Interfaces;
using FlipZA.Core.Marketplaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlipZA.Infrastructure.Takealot;

/// <summary>
/// Reads Takealot's public JSON API (the same one its website calls). Robots.txt checks,
/// throttling and retries are applied by the HttpClient pipeline, not here.
/// </summary>
public sealed class TakealotClient(
    HttpClient httpClient,
    IOptions<TakealotOptions> options,
    TimeProvider timeProvider,
    ILogger<TakealotClient> logger) : IMarketplaceSource
{
    private readonly string _apiVersion = options.Value.ApiVersion;

    public Marketplace Marketplace => Marketplace.Takealot;

    public async Task<IReadOnlyList<MarketplaceSearchHit>> SearchAsync(
        MarketplaceSearch search, int maxPages, CancellationToken cancellationToken)
    {
        var hits = new List<MarketplaceSearchHit>();
        string? after = null;

        for (var page = 1; page <= maxPages; page++)
        {
            var url = $"rest/{_apiVersion}/searches/products?qsearch={Uri.EscapeDataString(search.Query)}";
            if (search.DepartmentId is { } department)
                url += $"&filter=Type:{department}";
            if (after is not null)
                url += $"&after={Uri.EscapeDataString(after)}";

            var json = await httpClient.GetStringAsync(url, cancellationToken);
            var result = TakealotParser.ParseSearch(json);
            hits.AddRange(result.Hits);

            logger.LogDebug("Search '{Query}' (department {Department}) page {Page}: {Count} products ({Total} total)",
                search.Query, search.DepartmentId, page, result.Hits.Count, result.TotalFound);

            after = result.NextPageToken;
            if (after is null || result.Hits.Count == 0)
                break;
        }

        return hits.DistinctBy(h => h.ExternalId).ToList();
    }

    public async Task<ScrapedListing?> GetListingAsync(string externalId, CancellationToken cancellationToken)
    {
        var plid = externalId.StartsWith("PLID", StringComparison.OrdinalIgnoreCase) ? externalId : $"PLID{externalId}";
        using var response = await httpClient.GetAsync(
            $"rest/{_apiVersion}/product-details/{plid}?platform=desktop", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("Takealot product {Plid} not found", plid);
            return null;
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return TakealotParser.ParseProductDetails(json, timeProvider.GetUtcNow().UtcDateTime);
    }
}
