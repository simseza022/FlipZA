using FlipZA.Engine.Configurations;
using FlipZA.Engine.Jobs;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

var engineSettings = builder.Configuration
    .GetSection(EngineSettings.SectionName)
    .Get<EngineSettings>() ?? new EngineSettings();

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
host.Run();
