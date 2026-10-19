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
| `Searches` | 20 gadget searches | List of `{ "Query": "...", "Department": id }`. `Department` is optional. |
| `MaxPagesPerSearch` | `2` | Result pages per search (36 products each) |
| `MinPrice` / `MaxPrice` | `200` / `3000` | Rand price band for candidates |
| `MinReviewCount` | `20` | Minimum reviews (sales-velocity proxy) |
| `RequireInStock` | `true` | Skip out-of-stock products |
| `MaxTrackedProducts` | `150` | Size of the tracked set (excluding `ProductIds`) |

See [Takealot scraper](scraper.md#discovery-job-choosing-what-to-track) for how these are applied and for department ids.

## `TakealotScraper:Refresh`: price history

| Key | Default | Description |
|---|---|---|
| `IntervalHours` | `12` | How often each tracked product is re-checked |
| `InitialDelayMinutes` | `30` | Delay before the first refresh after startup, so it follows discovery |
| `MaxProductsPerRun` | `150` | Cap on products refreshed per run (stalest first) |

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

defaults:  (20 × 2 + ~10) × 2  +  150 × 2  ≈  400 requests/day
```

Each run's duration is roughly `requests × MinRequestIntervalMs`; a 150-product refresh takes about 6–7 minutes.
