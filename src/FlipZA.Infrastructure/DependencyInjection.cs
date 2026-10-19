using FlipZA.Core.Interfaces;
using FlipZA.Infrastructure.Http;
using FlipZA.Infrastructure.Persistence;
using FlipZA.Infrastructure.Takealot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlipZA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FlipZaDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("FlipZA") ?? "Data Source=flipza.db"));
        services.AddScoped<IListingRepository, ListingRepository>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<HostThrottle>();
        services.AddSingleton<RobotsTxtCache>();

        services.Configure<TakealotOptions>(configuration.GetSection(TakealotOptions.SectionName));

        services.AddHttpClient(RobotsTxtCache.HttpClientName, (sp, client) =>
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    sp.GetRequiredService<IOptions<TakealotOptions>>().Value.UserAgent))
            .AddHttpMessageHandler(CreateThrottlingHandler);

        // Handlers run in registration order: robots.txt is checked once per call, then retries,
        // and the throttle sits innermost so every attempt (including retries) is spaced out.
        var takealot = services.AddHttpClient<TakealotClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TakealotOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseAddress);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });
        takealot.AddHttpMessageHandler(sp => new RobotsTxtHandler(
            sp.GetRequiredService<RobotsTxtCache>(),
            sp.GetRequiredService<IOptions<TakealotOptions>>().Value.ProductToken));
        takealot.AddStandardResilienceHandler(resilience =>
        {
            resilience.Retry.MaxRetryAttempts = 3;
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
            resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(1);
        });
        takealot.AddHttpMessageHandler(CreateThrottlingHandler);

        return services;
    }

    private static ThrottlingHandler CreateThrottlingHandler(IServiceProvider sp) =>
        new(sp.GetRequiredService<HostThrottle>(),
            TimeSpan.FromMilliseconds(sp.GetRequiredService<IOptions<TakealotOptions>>().Value.MinRequestIntervalMs));
}
