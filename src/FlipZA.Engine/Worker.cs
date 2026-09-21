namespace FlipZA.Engine;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public Worker(
        ILogger<Worker> logger, 
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FlipZA Engine initialized and running on VPS.");

        // Read execution interval from appsettings.json (e.g., every 15 minutes)
        int intervalMinutes = _configuration.GetValue<int>("EngineSettings:ScrapeIntervalMinutes", 15);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting market scan cycle at: {time}", DateTimeOffset.Now);

            try
            {
                // Create a Scoped DI container for DB Context & HttpClient per run
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Resolve infrastructure services here (e.g., TakealotScraper, MarginEngine)
                    // var scraper = scope.ServiceProvider.GetRequiredService<ITakealotScraper>();
                    // await scraper.RunScanAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during FlipZA scraping execution cycle.");
            }

            _logger.LogInformation("Scan cycle complete. Sleeping for {minutes} minutes...", intervalMinutes);

            // Wait for the next scheduled interval
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}