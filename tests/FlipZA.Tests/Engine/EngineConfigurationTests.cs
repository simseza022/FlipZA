using FlipZA.Core.Marketplaces;
using FlipZA.Engine.Configurations;
using Microsoft.Extensions.Configuration;

namespace FlipZA.Tests.Engine;

/// <summary>Guards the shipped appsettings.json against typos that would only show up on the server.</summary>
public class EngineConfigurationTests
{
    private static readonly TakealotScraperSettings Settings = LoadEngineSettings();

    [Fact]
    public void EveryImportSearch_UsesAConfiguredProfile()
    {
        var missing = Settings.Discovery.Searches
            .Where(s => s.Channel == SourcingChannel.Import)
            .Where(s => !Settings.Discovery.ImportProfiles.ContainsKey(s.Profile))
            .Select(s => $"{s.Query} -> {s.Profile}")
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void ImportProfiles_LoadTheirListsFromConfiguration()
    {
        var fashion = Settings.Discovery.ImportProfiles["fashion"]; // profile names are case-insensitive

        Assert.Contains("Guess", fashion.ExcludedBrands);
        Assert.Contains("replica", fashion.ExcludedTitleKeywords);
        Assert.Contains("Generic", fashion.GenericBrandNames);
        Assert.Contains("bluetooth", Settings.Discovery.ImportProfiles["Electronics"].ExcludedTitleKeywords);
    }

    [Fact]
    public void SearchQueries_AreUnique()
    {
        var duplicates = Settings.Discovery.Searches
            .GroupBy(s => (s.Query.ToLowerInvariant(), s.Department))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.Item1)
            .ToList();

        Assert.Empty(duplicates);
    }

    private static TakealotScraperSettings LoadEngineSettings()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FlipZA.sln")))
            directory = directory.Parent;

        var appSettings = Path.Combine(directory!.FullName, "src", "FlipZA.Engine", "appsettings.json");
        var configuration = new ConfigurationBuilder().AddJsonFile(appSettings).Build();

        return configuration.GetSection(TakealotScraperSettings.SectionName).Get<TakealotScraperSettings>()!;
    }
}
