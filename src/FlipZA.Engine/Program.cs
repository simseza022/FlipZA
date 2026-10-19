using FlipZA.Engine.Configurations;
using FlipZA.Engine.Jobs;
using FlipZA.Infrastructure;
using FlipZA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

var engineSettings = builder.Configuration
    .GetSection(EngineSettings.SectionName)
    .Get<EngineSettings>() ?? new EngineSettings();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<TakealotScraperSettings>(
    builder.Configuration.GetSection(TakealotScraperSettings.SectionName));

builder.Services.AddQuartz(q =>
{
    var takealotJobKey = new JobKey(nameof(TakealotScraperJob));

    q.AddJob<TakealotScraperJob>(opts => opts.WithIdentity(takealotJobKey));
    q.AddTrigger(opts => opts
        .ForJob(takealotJobKey)
        .WithIdentity($"{nameof(TakealotScraperJob)}-trigger")
        .StartNow()
        .WithSimpleSchedule(s => s
            .WithIntervalInMinutes(engineSettings.ScrapeIntervalMinutes)
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
