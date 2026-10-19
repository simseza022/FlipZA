using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;

namespace FlipZA.Infrastructure.Http;

public sealed class RobotsDisallowedException(Uri uri)
    : Exception($"robots.txt disallows fetching {uri}")
{
    public Uri Uri { get; } = uri;
}

/// <summary>
/// Caches robots.txt per host and blocks requests the site has disallowed.
/// Registered as a singleton so the cache survives HttpClient handler rotation.
/// </summary>
public sealed class RobotsTxtCache(IHttpClientFactory httpClientFactory, ILogger<RobotsTxtCache> logger)
{
    public const string HttpClientName = "robots-txt";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, (RobotsTxtRules Rules, DateTime ExpiresAtUtc)> _cache = new();
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    public async Task<bool> IsAllowedAsync(Uri uri, string productToken, CancellationToken cancellationToken)
    {
        var rules = await GetRulesAsync(uri, productToken, cancellationToken);
        return rules.IsAllowed(uri.PathAndQuery);
    }

    private async Task<RobotsTxtRules> GetRulesAsync(Uri uri, string productToken, CancellationToken cancellationToken)
    {
        var host = uri.GetLeftPart(UriPartial.Authority);
        if (_cache.TryGetValue(host, out var cached) && cached.ExpiresAtUtc > DateTime.UtcNow)
            return cached.Rules;

        await _fetchLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(host, out cached) && cached.ExpiresAtUtc > DateTime.UtcNow)
                return cached.Rules;

            var rules = await FetchRulesAsync(host, productToken, cancellationToken);
            _cache[host] = (rules, DateTime.UtcNow.Add(CacheDuration));
            return rules;
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    private async Task<RobotsTxtRules> FetchRulesAsync(string host, string productToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync($"{host}/robots.txt", cancellationToken);

        // RFC 9309: a 4xx means no restrictions; server errors mean assume everything is disallowed.
        if ((int)response.StatusCode is >= 400 and < 500)
        {
            logger.LogInformation("No robots.txt at {Host} ({Status}); treating as allow-all", host, response.StatusCode);
            return RobotsTxtRules.AllowAll;
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogWarning("robots.txt at {Host} returned {Status}; treating as disallow-all", host, response.StatusCode);
            return RobotsTxtRules.DisallowAll;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Loaded robots.txt for {Host}", host);
        return RobotsTxtRules.Parse(content, productToken);
    }
}

public sealed class RobotsTxtHandler(RobotsTxtCache cache, string productToken) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri ?? throw new InvalidOperationException("Request has no URI.");
        if (!await cache.IsAllowedAsync(uri, productToken, cancellationToken))
            throw new RobotsDisallowedException(uri);

        return await base.SendAsync(request, cancellationToken);
    }
}
