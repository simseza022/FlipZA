namespace FlipZA.Infrastructure.Takealot;

public class TakealotOptions
{
    public const string SectionName = "Takealot";

    public string BaseAddress { get; set; } = "https://api.takealot.com/";

    /// <summary>Takealot versions its REST paths (e.g. "v-1-16-0"); bump here when they move on.</summary>
    public string ApiVersion { get; set; } = "v-1-16-0";

    /// <summary>Product token matched against robots.txt user-agent groups.</summary>
    public string ProductToken { get; set; } = "FlipZA";

    public string UserAgent { get; set; } = "FlipZA/0.1 (+https://github.com/simseza022/FlipZA)";

    /// <summary>Minimum gap between requests to the same host, including retries.</summary>
    public int MinRequestIntervalMs { get; set; } = 2500;
}
