using FlipZA.Engine.Configurations;
using FlipZA.Engine.Jobs;
using FlipZA.Engine.Services;
using FlipZA.Infrastructure;
using FlipZA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

var scraperSettings = builder.Configuration
    .GetSection(TakealotScraperSettings.SectionName)
    .Get<TakealotScraperSettings>() ?? new TakealotScraperSettings();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<TakealotScraperSettings>(
    builder.Configuration.GetSection(TakealotScraperSettings.SectionName));
builder.Services.Configure<DealSettings>(builder.Configuration.GetSection(DealSettings.SectionName));
builder.Services.AddSingleton<ListingPricer>();

builder.Services.AddQuartz(q =>
{
    var discoveryKey = new JobKey(nameof(TakealotDiscoveryJob));
    q.AddJob<TakealotDiscoveryJob>(opts => opts.WithIdentity(discoveryKey));
    q.AddTrigger(opts => opts
        .ForJob(discoveryKey)
        .WithIdentity($"{nameof(TakealotDiscoveryJob)}-trigger")
        .StartNow()
        .WithSimpleSchedule(s => s
            .WithIntervalInHours(scraperSettings.Discovery.IntervalHours)
            .RepeatForever()));

    // Starts after discovery has had time to pick the first tracked set.
    var refreshKey = new JobKey(nameof(TakealotRefreshJob));
    q.AddJob<TakealotRefreshJob>(opts => opts.WithIdentity(refreshKey));
    q.AddTrigger(opts => opts
        .ForJob(refreshKey)
        .WithIdentity($"{nameof(TakealotRefreshJob)}-trigger")
        .StartAt(DateBuilder.FutureDate(scraperSettings.Refresh.InitialDelayMinutes, IntervalUnit.Minute))
        .WithSimpleSchedule(s => s
            .WithIntervalInHours(scraperSettings.Refresh.IntervalHours)
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlipZaDbContext>().Database;
    await db.MigrateAsync();

    // WAL lets readers (e.g. the API) query while the Engine writes; the setting persists in the file.
    await db.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}

await host.RunAsync();
