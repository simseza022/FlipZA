using FlipZA.Core.Marketplaces;

namespace FlipZA.Engine.Configurations;

public class TakealotScraperSettings
{
    public const string SectionName = "TakealotScraper";

    public DiscoverySettings Discovery { get; set; } = new();
    public RefreshSettings Refresh { get; set; } = new();

    /// <summary>Specific products to always track, e.g. "PLID98945485".</summary>
    public List<string> ProductIds { get; set; } = new();

    /// <summary>
    /// Finds candidate products via search and picks the tracked set: the most-reviewed
    /// products that pass the filters below.
    /// </summary>
    public class DiscoverySettings
    {
        public int IntervalHours { get; set; } = 12;
        public List<SearchSettings> Searches { get; set; } = new();

        /// <summary>Search result pages (36 products each) to read per search.</summary>
        public int MaxPagesPerSearch { get; set; } = 2;

        public decimal MinPrice { get; set; } = 200;
        public decimal MaxPrice { get; set; } = 3000;

        /// <summary>Review count is the best public proxy for sales velocity.</summary>
        public int MinReviewCount { get; set; } = 20;

        public bool RequireInStock { get; set; } = true;

        /// <summary>Upper bound on tracked products; keep it refreshable within one refresh interval.</summary>
        public int MaxTrackedProducts { get; set; } = 150;

        /// <summary>
        /// Named filter sets for searches in the <see cref="SourcingChannel.Import"/> channel, e.g.
        /// "Electronics" and "Fashion"; each import search picks one with <see cref="SearchSettings.Profile"/>.
        /// </summary>
        public Dictionary<string, ImportFilterSettings> ImportProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class SearchSettings
    {
        public string Query { get; set; } = string.Empty;

        /// <summary>Takealot department id (the search "Type" filter), e.g. 16 = Cellphones &amp; Wearables.</summary>
        public int? Department { get; set; }

        /// <summary>Import: generic, simple-to-import products only. Local: any brand (SA wholesale stock).</summary>
        public SourcingChannel Channel { get; set; } = SourcingChannel.Local;

        /// <summary>Which entry of <see cref="DiscoverySettings.ImportProfiles"/> filters an Import search.</summary>
        public string Profile { get; set; } = "Electronics";
    }

    // Lists are empty here on purpose: the configuration binder appends to (rather than replaces)
    // pre-filled lists, so the defaults live in appsettings.json.
    public class ImportFilterSettings
    {
        /// <summary>Brand values that mean "no brand", e.g. "Generic", "Unbranded".</summary>
        public List<string> GenericBrandNames { get; set; } = new();

        /// <summary>Established brands to skip: buying them from overseas suppliers risks counterfeits.</summary>
        public List<string> ExcludedBrands { get; set; } = new();

        /// <summary>When true, only products with no brand qualify (small private labels are skipped too).</summary>
        public bool UnbrandedOnly { get; set; }

        /// <summary>
        /// Title words that rule a product out, e.g. for electronics radio (ICASA approval), lithium
        /// batteries (dangerous goods freight) or mains chargers (NRCS approval); for fashion,
        /// counterfeit signals such as "replica" or "inspired".
        /// </summary>
        public List<string> ExcludedTitleKeywords { get; set; } = new();
    }

    /// <summary>Re-scrapes tracked products to build price and stock history.</summary>
    public class RefreshSettings
    {
        public int IntervalHours { get; set; } = 12;

        /// <summary>
        /// Delay before the first refresh, so it follows the first discovery run instead of
        /// colliding with it; while one job's interval is a multiple of the other's, they stay this far apart.
        /// </summary>
        public int InitialDelayMinutes { get; set; } = 30;

        /// <summary>Hard cap on product lookups per run, to bound run time and load on Takealot.</summary>
        public int MaxProductsPerRun { get; set; } = 150;
    }
}
