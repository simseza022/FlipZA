using System.Collections.Concurrent;

namespace FlipZA.Infrastructure.Http;

/// <summary>
/// Enforces a minimum gap between requests to the same host, across all clients and retries.
/// Registered as a singleton so the timing survives HttpClient handler rotation.
/// </summary>
public sealed class HostThrottle(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, HostSlot> _hosts = new();

    public async Task WaitTurnAsync(string host, TimeSpan minimumGap, CancellationToken cancellationToken)
    {
        var slot = _hosts.GetOrAdd(host, _ => new HostSlot());

        await slot.Lock.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            var wait = slot.LastRequestAt + minimumGap - now;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, timeProvider, cancellationToken);

            slot.LastRequestAt = timeProvider.GetUtcNow();
        }
        finally
        {
            slot.Lock.Release();
        }
    }

    private sealed class HostSlot
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public DateTimeOffset LastRequestAt { get; set; } = DateTimeOffset.MinValue;
    }
}

public sealed class ThrottlingHandler(HostThrottle throttle, TimeSpan minimumGap) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { } uri)
            await throttle.WaitTurnAsync(uri.Host, minimumGap, cancellationToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
