using FlipZA.Core.Interfaces;
using FlipZA.Engine.Configurations;
using FlipZA.Infrastructure.Takealot;
using Microsoft.Extensions.Options;
using Quartz;

namespace FlipZA.Engine.Jobs;

// Quartz creates a fresh DI scope per execution, so scoped services
// (DbContext, scrapers) can be injected straight into the constructor.
[DisallowConcurrentExecution]
public class TakealotScraperJob : IJob
{
    private readonly TakealotClient _takealot;
    private readonly IListingRepository _repository;
    private readonly TakealotScraperSettings _settings;
    private readonly ILogger<TakealotScraperJob> _logger;

    public TakealotScraperJob(
        TakealotClient takealot,
        IListingRepository repository,
        IOptions<TakealotScraperSettings> settings,
        ILogger<TakealotScraperJob> logger)
    {
        _takealot = takealot;
        _repository = repository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        _logger.LogInformation("Starting Takealot scan cycle at: {time}", DateTimeOffset.Now);

        var productIds = new List<string>(_settings.ProductIds);
        foreach (var query in _settings.SearchQueries)
        {
            try
            {
                var found = await _takealot.SearchAsync(query, _settings.MaxPagesPerQuery, ct);
                _logger.LogInformation("Search '{Query}' returned {Count} products", query, found.Count);
                productIds.AddRange(found);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Takealot search '{Query}' failed", query);
            }
        }

        var targets = productIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(_settings.MaxProductsPerRun)
            .ToList();

        int saved = 0, missing = 0, failed = 0;
        foreach (var plid in targets)
        {
            try
            {
                var listing = await _takealot.GetListingAsync(plid, ct);
                if (listing is null)
                {
                    missing++;
                    continue;
                }

                await _repository.SaveAsync(listing, ct);
                saved++;
                _logger.LogDebug("Saved {Plid}: {Title} @ R{Price}", plid, listing.Product.Title, listing.Snapshot.Price);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogError(ex, "Failed to scrape Takealot product {Plid}", plid);
            }
        }

        _logger.LogInformation(
            "Takealot scan cycle complete: {Saved}/{Total} saved, {Missing} not found, {Failed} failed. Next run at: {Next}",
            saved, targets.Count, missing, failed, context.NextFireTimeUtc?.ToLocalTime());

        if (targets.Count > 0 && failed == targets.Count)
            _logger.LogCritical("Every Takealot product failed this run; the API or response format may have changed");
    }
}
