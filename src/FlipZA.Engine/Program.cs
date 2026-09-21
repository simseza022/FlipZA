using FlipZA.Engine.Jobs;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TakealotScaperJob>();

var host = builder.Build();
host.Run();
