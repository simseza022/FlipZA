using Quartz;

namespace FlipZA.Engine.Jobs;

// Quartz creates a fresh DI scope per execution, so scoped services
// (DbContext, scrapers) can be injected straight into the constructor.
[DisallowConcurrentExecution]
public class TakealotScraperJob : IJob
{
    private readonly ILogger<TakealotScraperJob> _logger;

    public TakealotScraperJob(ILogger<TakealotScraperJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting Takealot scan cycle at: {time}", DateTimeOffset.Now);

        try
        {
            // Resolve infrastructure services via constructor injection (e.g., TakealotScraper, MarginEngine)
            // await _scraper.RunScanAsync(context.CancellationToken);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Takealot scan cycle.");
        }

        _logger.LogInformation("Takealot scan cycle complete. Next run at: {next}", context.NextFireTimeUtc?.ToLocalTime());
    }
}
