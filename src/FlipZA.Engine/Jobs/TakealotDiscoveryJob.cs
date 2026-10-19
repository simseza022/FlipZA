using FlipZA.Core.Interfaces;
using FlipZA.Core.Marketplaces;
using FlipZA.Engine.Configurations;
using FlipZA.Infrastructure.Takealot;
using Microsoft.Extensions.Options;
using Quartz;

namespace FlipZA.Engine.Jobs;

/// <summary>
/// Runs the configured gadget searches and re-picks the tracked set: in-stock products in the
/// price band with the most reviews. Search results carry price, stock and reviews, so filtering
/// costs no extra requests; only newly tracked products need a full product-details fetch.
/// </summary>
[DisallowConcurrentExecution]
public class TakealotDiscoveryJob : IJob
{
    private readonly TakealotClient _takealot;
    private readonly IListingRepository _repository;
    private readonly TakealotScraperSettings _settings;
    private readonly ILogger<TakealotDiscoveryJob> _logger;

    public TakealotDiscoveryJob(
        TakealotClient takealot,
        IListingRepository repository,
        IOptions<TakealotScraperSettings> settings,
        ILogger<TakealotDiscoveryJob> logger)
    {
        _takealot = takealot;
        _repository = repository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var discovery = _settings.Discovery;
        _logger.LogInformation("Starting Takealot discovery: {Count} searches", discovery.Searches.Count);

        var candidatesPerSearch = new List<List<MarketplaceSearchHit>>();
        var hitCount = 0;
        var failedSearches = 0;
        foreach (var search in discovery.Searches)
        {
            try
            {
                var found = await _takealot.SearchAsync(
                    new MarketplaceSearch(search.Query, search.Department), discovery.MaxPagesPerSearch, ct);
                hitCount += found.Count;
                candidatesPerSearch.Add(found.Where(IsCandidate).OrderByDescending(h => h.ReviewCount).ToList());
                _logger.LogInformation("Search '{Query}' returned {Count} products", search.Query, found.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failedSearches++;
                _logger.LogError(ex, "Takealot search '{Query}' failed", search.Query);
            }
        }

        var tracked = _settings.ProductIds
            .Concat(PickTrackedSet(candidatesPerSearch, discovery.MaxTrackedProducts))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // A partial discovery run would wrongly drop products, so only prune after a clean run.
        var untrackOthers = failedSearches == 0;
        var newIds = await _repository.SetTrackedAsync(Marketplace.Takealot, tracked, untrackOthers, ct);

        int saved = 0, failed = 0;
        foreach (var plid in newIds)
        {
            try
            {
                var listing = await _takealot.GetListingAsync(plid, ct);
                if (listing is null)
                    continue;

                listing.Product.IsTracked = true;
                await _repository.SaveAsync(listing, ct);
                saved++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogError(ex, "Failed to scrape Takealot product {Plid}", plid);
            }
        }

        _logger.LogInformation(
            "Takealot discovery complete: {Hits} search hits, {Tracked} tracked ({New} new, {Saved} saved, {Failed} failed), {FailedSearches} failed searches{Pruned}",
            hitCount, tracked.Count, newIds.Count, saved, failed, failedSearches,
            untrackOthers ? string.Empty : "; skipped pruning untracked products");
    }

    private bool IsCandidate(MarketplaceSearchHit hit)
    {
        var discovery = _settings.Discovery;
        return hit.Price is { } price && price >= discovery.MinPrice && price <= discovery.MaxPrice
               && hit.ReviewCount >= discovery.MinReviewCount
               && (hit.IsInStock || !discovery.RequireInStock);
    }

    /// <summary>
    /// Gives every search an equal share of the slots (its most-reviewed candidates), so a few
    /// high-review categories can't crowd out the rest; leftover slots go to the most-reviewed overall.
    /// </summary>
    public static List<string> PickTrackedSet(IReadOnlyList<List<MarketplaceSearchHit>> candidatesPerSearch, int maxTracked)
    {
        if (candidatesPerSearch.Count == 0 || maxTracked <= 0)
            return [];

        var picked = new List<string>();
        var share = Math.Max(1, maxTracked / candidatesPerSearch.Count);

        foreach (var candidates in candidatesPerSearch)
        {
            picked.AddRange(candidates
                .Select(h => h.ExternalId)
                .Where(id => !picked.Contains(id))
                .Take(share)
                .ToList());
        }

        var leftovers = candidatesPerSearch
            .SelectMany(c => c)
            .Where(h => !picked.Contains(h.ExternalId))
            .OrderByDescending(h => h.ReviewCount)
            .Select(h => h.ExternalId)
            .Distinct();

        return picked.Concat(leftovers).Take(maxTracked).ToList();
    }
}
