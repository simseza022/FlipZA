namespace FlipZA.Core.Marketplaces;

/// <summary>How a tracked product would be sourced, which decides the filters it must pass.</summary>
public enum SourcingChannel
{
    /// <summary>Generic or private-label stock imported (e.g. from Alibaba): no established brands, simple to import.</summary>
    Import = 1,

    /// <summary>Stock bought from South African wholesalers: any brand.</summary>
    Local = 2,

    /// <summary>Listed explicitly in configuration.</summary>
    Manual = 3
}

/// <summary>A product the discovery run wants tracked, and the channel it was chosen for.</summary>
public sealed record TrackingTarget(string ExternalId, SourcingChannel Channel);
