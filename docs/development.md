# Development

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Optional: Docker (to test the container), DataGrip or DB Browser for SQLite

## Build, test, run

```bash
dotnet tool restore                       # installs dotnet-ef 9.0.20 from .config/dotnet-tools.json
dotnet build FlipZA.sln
dotnet test
dotnet run --project src/FlipZA.Engine    # creates src/FlipZA.Engine/flipza.db
```

The local database is git-ignored (`*.db`).

### Quick local runs without editing config

Environment variables override `appsettings.json` (see [Configuration](configuration.md)). For a short trial run that tracks only 5 products, refreshes after 1 minute and writes to a throwaway database:

```bash
cd src/FlipZA.Engine
ConnectionStrings__FlipZA="Data Source=/tmp/flipza-trial.db" \
TakealotScraper__Discovery__MaxPagesPerSearch=1 \
TakealotScraper__Discovery__MaxTrackedProducts=5 \
TakealotScraper__Refresh__InitialDelayMinutes=1 \
dotnet run
```

This still makes real requests to Takealot (about 30), so keep trial runs occasional.

## Tests

Tests live in `tests/FlipZA.Tests` and never touch the network.

| Folder | Covers |
|---|---|
| `Takealot/` | Parsing search and product-details responses, including missing-field tolerance |
| `Http/` | robots.txt rules against Takealot's real robots files and RFC edge cases |
| `Persistence/` | Upserts, snapshot history and tracked-set changes, on an in-memory SQLite database with the real migrations |
| `Engine/` | Tracked-set selection and snapshot pricing |
| `Deals/` | Every branch of the deal calculator, with worked examples |

**Fixtures** in `tests/FlipZA.Tests/Fixtures/Takealot/` are real responses saved from Takealot:

| File | Contents |
|---|---|
| `product-details-retail.json` | Russell Hobbs air fryer sold by Takealot, with 6 competing offers |
| `product-details-marketplace.json` | HiMost air fryer whose buy box is held by a third-party seller |
| `search.json` | First 3 results of an "air fryer" search |
| `robots-api.txt`, `robots-www.txt` | Takealot's robots.txt files |

When Takealot changes its response format, save a fresh response over the matching fixture, update the parser, and update the assertions.

## Database migrations

The schema is defined by the entity classes in `FlipZA.Core` plus the configurations in `src/FlipZA.Infrastructure/Persistence/Configurations/`. After changing either:

```bash
dotnet ef migrations add <DescriptiveName> \
  --project src/FlipZA.Infrastructure \
  --output-dir Persistence/Migrations
```

`DesignTimeDbContextFactory` lets `dotnet ef` build the context without starting the Engine. The Engine applies pending migrations on startup, locally and on the VPS.

## Conventions

- **Core has no dependencies.** No NuGet packages, no EF attributes; persistence mapping lives in Infrastructure.
- **Money is `decimal`** in code; never use `double` for amounts.
- **Timestamps are UTC** (`DateTime` with `DateTimeKind.Utc`).
- **Parsers tolerate missing data.** Use the `JsonElementExtensions` helpers (`Prop`, `AsString`, `AsDecimal`, ...), which return null instead of throwing.
- **No HTTP politeness in callers.** Robots, retries and throttling belong in the HTTP pipeline (`DependencyInjection.cs`), so every client gets them.
- **One job per concern.** Jobs orchestrate; parsing, persistence and maths live in their own classes so they can be tested in isolation.

## Adding another marketplace (e.g. Makro)

1. Add the marketplace to the `Marketplace` enum in Core.
2. In Infrastructure, add `Makro/MakroClient.cs` implementing `IMarketplaceSource`, plus a parser that maps responses onto `MarketplaceProduct` and `ListingSnapshot`.
3. Register its `HttpClient` in `DependencyInjection.cs` with the same handler chain (`RobotsTxtHandler`, resilience, `ThrottlingHandler`).
4. Save real responses as fixtures and write parser tests first.
5. Add discovery and refresh jobs (or generalise the Takealot ones) and their settings in the Engine.
6. Add Makro's fee structure as its own `FeeSchedule` configuration.

## Useful references inside the code

| To understand | Start at |
|---|---|
| Scheduling and startup | `src/FlipZA.Engine/Program.cs` |
| How products are chosen | `src/FlipZA.Engine/Jobs/TakealotDiscoveryJob.cs` |
| How prices are recorded | `src/FlipZA.Engine/Jobs/TakealotRefreshJob.cs` |
| Takealot JSON → entities | `src/FlipZA.Infrastructure/Takealot/TakealotParser.cs` |
| HTTP pipeline | `src/FlipZA.Infrastructure/DependencyInjection.cs` |
| Deal maths | `src/FlipZA.Core/Deals/DealCalculator.cs` |
