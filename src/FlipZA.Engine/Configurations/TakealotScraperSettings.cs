namespace FlipZA.Engine.Configurations;

public class TakealotScraperSettings
{
    public const string SectionName = "TakealotScraper";

    /// <summary>Keyword searches whose results are scanned each run.</summary>
    public List<string> SearchQueries { get; set; } = new();

    /// <summary>Search result pages (36 products each) to read per query.</summary>
    public int MaxPagesPerQuery { get; set; } = 1;

    /// <summary>Specific products to always track, e.g. "PLID98945485".</summary>
    public List<string> ProductIds { get; set; } = new();

    /// <summary>Hard cap on product lookups per run, to bound run time and load on Takealot.</summary>
    public int MaxProductsPerRun { get; set; } = 100;
}
