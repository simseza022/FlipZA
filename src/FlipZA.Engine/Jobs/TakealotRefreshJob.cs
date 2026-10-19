using FlipZA.Core.Interfaces;
using FlipZA.Core.Marketplaces;
using FlipZA.Engine.Configurations;
using FlipZA.Infrastructure.Takealot;
using Microsoft.Extensions.Options;
using Quartz;

namespace FlipZA.Engine.Jobs;

/// <summary>
/// Re-scrapes the tracked products, least recently refreshed first, appending a price/stock
/// snapshot for each. Products Takealot no longer lists are untracked.
/// </summary>
// Quartz creates a fresh DI scope per execution, so scoped services
// (DbContext, scrapers) can be injected straight into the constructor.
[DisallowConcurrentExecution]
public class TakealotRefreshJob : IJob
{
    private readonly TakealotClient _takealot;
    private readonly IListingRepository _repository;
    private readonly TakealotScraperSettings _settings;
    private readonly ILogger<TakealotRefreshJob> _logger;

    public TakealotRefreshJob(
        TakealotClient takealot,
        IListingRepository repository,
        IOptions<TakealotScraperSettings> settings,
        ILogger<TakealotRefreshJob> logger)
    {
        _takealot = takealot;
        _repository = repository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var targets = await _repository.GetTrackedAsync(Marketplace.Takealot, _settings.Refresh.MaxProductsPerRun, ct);
        _logger.LogInformation("Starting Takealot refresh of {Count} tracked products", targets.Count);

        int saved = 0, missing = 0, failed = 0;
        foreach (var plid in targets)
        {
            try
            {
                var listing = await _takealot.GetListingAsync(plid, ct);
                if (listing is null)
                {
                    missing++;
                    await _repository.UntrackAsync(Marketplace.Takealot, plid, ct);
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
            "Takealot refresh complete: {Saved}/{Total} saved, {Missing} delisted, {Failed} failed. Next run at: {Next}",
            saved, targets.Count, missing, failed, context.NextFireTimeUtc?.ToLocalTime());

        if (targets.Count > 0 && failed == targets.Count)
            _logger.LogCritical("Every Takealot product failed this run; the API or response format may have changed");
    }
}
