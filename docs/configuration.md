# Configuration

All Engine settings live in `src/FlipZA.Engine/appsettings.json`. Any setting can be overridden with an **environment variable**, which is the easiest way to change a setting on the VPS without rebuilding.

## Overriding with environment variables

Replace each `:` (or nesting level) with a double underscore `__`. Array items use their index.

| Setting | Environment variable |
|---|---|
| `Deals:Reseller:IsVatRegistered` | `Deals__Reseller__IsVatRegistered=false` |
| `TakealotScraper:Refresh:IntervalHours` | `TakealotScraper__Refresh__IntervalHours=6` |
| `TakealotScraper:ProductIds[0]` | `TakealotScraper__ProductIds__0=PLID98945485` |

On the VPS, add them under `environment:` in `docker-compose.yml`, then run `docker compose up -d`.

Most settings are read when the Engine starts, so **restart after changing them**.

---

## `ConnectionStrings`

| Key | Default | Description |
|---|---|---|
| `FlipZA` | `Data Source=flipza.db` | SQLite file. The Docker image overrides this to `Data Source=/data/flipza.db`. |

## `Takealot`: API client

| Key | Default | Description |
|---|---|---|
| `BaseAddress` | `https://api.takealot.com/` | API host |
| `ApiVersion` | `v-1-16-0` | Version segment in API paths. Update when Takealot retires it. |
| `ProductToken` | `FlipZA` | Name matched against `User-agent:` groups in robots.txt |
| `UserAgent` | `FlipZA/0.1 (+https://github.com/simseza022/FlipZA)` | Sent with every request |
| `MinRequestIntervalMs` | `2500` | Minimum gap between requests to the same host, in milliseconds |

## `TakealotScraper:Discovery`: choosing what to track

| Key | Default | Description |
|---|---|---|
| `IntervalHours` | `12` | How often discovery runs (first run at startup) |
| `Searches` | 18 electronics import + 21 fashion import + 10 local searches | List of `{ "Query": "...", "Department": id, "Channel": "Import" \| "Local", "Profile": "Electronics" \| "Fashion" }`. `Department` is optional; `Channel` defaults to `Local`; `Profile` (Import only) defaults to `Electronics`. |
| `MaxPagesPerSearch` | `2` | Result pages per search (36 products each) |
| `MinPrice` / `MaxPrice` | `200` / `3000` | Rand price band for candidates |
| `MinReviewCount` | `20` | Minimum reviews (sales-velocity proxy) |
| `RequireInStock` | `true` | Skip out-of-stock products |
| `MaxTrackedProducts` | `245` | Size of the tracked set (excluding `ProductIds`) |

See [Takealot scraper](scraper.md#discovery-job-choosing-what-to-track) for how these are applied and for department ids.

## `TakealotScraper:Discovery:ImportProfiles`: filters for Import-channel searches

A dictionary of named profiles; each Import search picks one with `Profile`. Names are case-insensitive. Two are configured: `Electronics` and `Fashion`. Every profile has these keys:

| Key | Default (`Electronics` profile) | Description |
|---|---|---|
| `GenericBrandNames` | `Generic`, `Unbranded`, `No Brand`, `Non-Branded`, `OEM`, `No result` | Brand values treated as "no brand" (case-insensitive) |
| `ExcludedBrands` | 44 established brands (Apple, Samsung, Belkin, Anker, UGreen, Volkano, SanDisk, Edifier, ...) | Skipped because overseas stock of these risks being counterfeit |
| `UnbrandedOnly` | `false` | `true` also skips small private labels, keeping only products with no brand |
| `ExcludedTitleKeywords` | `bluetooth`, `wireless`, `wifi`, `wi-fi`, `smart`, `rechargeable`, `battery`, `mah`, `power bank`, `powerbank`, `wall charger`, `car charger`, `charger plug`, `solar` | Whole-word, case-insensitive title matches that signal radio, battery or mains-charger products |

The `Fashion` profile excludes about 50 fashion, luggage and watch brands (Gucci, Michael Kors, Guess, Polo, Samsonite, Casio, Ray-Ban, ...) and the title keywords `replica`, `inspired`, `dupe`, `1:1`, `smart` and `bluetooth`.

To add a profile (e.g. `Home`), add an entry under `ImportProfiles` and set `"Profile": "Home"` on its searches. A test (`EngineConfigurationTests`) fails if any Import search names a profile that doesn't exist.

Default lists are defined in `appsettings.json`, not in code: .NET's configuration binder adds configured items to pre-filled lists instead of replacing them. To change a list with environment variables, set each index (`TakealotScraper__Discovery__ImportProfiles__Fashion__ExcludedBrands__0`, `__1`, ...), or edit `appsettings.json`.

## `TakealotScraper:Refresh`: price history

| Key | Default | Description |
|---|---|---|
| `IntervalHours` | `12` | How often each tracked product is re-checked |
| `InitialDelayMinutes` | `30` | Delay before the first refresh after startup, so it follows discovery |
| `MaxProductsPerRun` | `245` | Cap on products refreshed per run (stalest first) |

## `TakealotScraper:ProductIds`

| Key | Default | Description |
|---|---|---|
| `ProductIds` | `[]` | PLIDs that are always tracked, e.g. `["PLID98945485"]` |

## `Deals`: deal calculator

See [Deal calculator](deal-calculator.md) for how each value is used.

### `Deals:Fees`

| Key | Default | Description |
|---|---|---|
| `DefaultSuccessFeeRate` | `0.15` | Takealot commission as a fraction of the sell price |
| `SuccessFeeRatesByDepartment` | `{}` | Overrides by department name, e.g. `{ "Gaming": 0.10 }` |
| `SuccessFeeRatesByCategory` | `{}` | Overrides by leaf category id; beats department rates |
| `FulfilmentFee` | `38` | Per-unit fulfilment fee in rand, excluding VAT |
| `VatRate` | `0.15` | South African VAT |

### `Deals:Reseller`

| Key | Default | Description |
|---|---|---|
| `IsVatRegistered` | `true` | Whether the reseller charges and claims VAT |
| `SupplierChargesVat` | `true` | Whether supplier prices include claimable VAT |
| `InboundShippingPerUnit` | `10` | **Placeholder.** Set to the real cost per unit of getting stock to Takealot's warehouse, including VAT. |

### `Deals:Gate`

| Key | Default | Description |
|---|---|---|
| `MinNetProfit` | `80` | Minimum profit per unit, in rand |
| `MinRoi` | `0.25` | Minimum ROI on landed cost (0.25 = 25%) |

## `Logging`

Standard .NET logging levels. Defaults keep HTTP client, EF Core and Polly noise at `Warning`. To see every product saved, set `Logging__LogLevel__FlipZA=Debug`.

## Load on Takealot

A rough request budget per day:

```text
discovery: (searches × MaxPagesPerSearch + newly tracked products) × (24 ÷ Discovery.IntervalHours)
refresh:   MaxProductsPerRun × (24 ÷ Refresh.IntervalHours)

defaults:  (49 × 2 + ~10) × 2  +  245 × 2  ≈  700 requests/day
```

Each run's duration is roughly `requests × MinRequestIntervalMs`; a 245-product refresh takes about 10–11 minutes.
