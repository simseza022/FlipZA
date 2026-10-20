# Takealot scraper

FlipZA reads Takealot through the same JSON API that Takealot's own website calls (`api.takealot.com`). This is faster and far lighter than rendering pages in a browser, and the data comes back already structured.

The scraper is split into two scheduled jobs:

| Job | Runs | Purpose | Requests per run |
|---|---|---|---|
| **Discovery** (`TakealotDiscoveryJob`) | Every 12 h, starting at startup | Decide *which* products to track | ~98 searches + details for newly tracked products |
| **Refresh** (`TakealotRefreshJob`) | Every 12 h, starting 30 min after startup | Record *current* price, stock and competition for every tracked product | 1 per tracked product (≤245) |

```text
time after startup:  0:00 discovery   0:30 refresh   12:00 discovery   12:30 refresh   ...
```

Both jobs are marked `[DisallowConcurrentExecution]`, so a job never overlaps itself, and the 30-minute offset keeps them from running at the same time. Total load is about **700 requests per day**.

---

## Takealot API endpoints used

| Purpose | Request |
|---|---|
| Search | `GET https://api.takealot.com/rest/{ApiVersion}/searches/products?qsearch={query}&filter=Type:{department}&after={pageToken}` |
| Product details | `GET https://api.takealot.com/rest/{ApiVersion}/product-details/{PLID}?platform=desktop` |

- `{ApiVersion}` is configurable (currently `v-1-16-0`). Takealot versions its API paths; when they move on, change the setting.
- Search returns **36 products per page**. The `after` token from `paging.next_is_after` fetches the next page.
- `filter=Type:{id}` limits a search to one **department**. Only one department per search is supported.
- A product is identified by its **PLID** (product line id), e.g. `PLID98945485`.

Department ids used by the gadget searches:

| Id | Department |
|---|---|
| 2 | Gaming |
| 13 | Computers & Tablets |
| 14 | Cameras |
| 15 | TV, Audio & Video |
| 16 | Cellphones & Wearables |

---

## Discovery job: choosing what to track

Takealot does not publish sales figures, so FlipZA uses **review count as a proxy for sales velocity**: products with many reviews have sold many units.

### Step 1: search
Each configured search (e.g. `"power bank"` in department 16) is run for up to `MaxPagesPerSearch` pages (default 2, so up to 72 products). Search results already include **price, review count, star rating and in-stock status**, so filtering costs no extra requests.

### Step 2: filter
Every search belongs to a **sourcing channel**, which decides how strict the filter is:

| Channel | Meant for | Searches (default) |
|---|---|---|
| **Import**, profile `Electronics` | Generic or private-label gadgets bought overseas (e.g. Alibaba) | 18: cables, phone holders and stands, laptop stands, watch straps, screen protectors, phone cases, USB hubs and adapters, HDMI cables, card readers, mouse pads, wired earphones, phone tripods, action-camera accessories, laptop sleeves, cable organisers |
| **Import**, profile `Fashion` | Generic or private-label fashion accessories and gifts bought overseas | 21: women's handbags, crossbody and tote bags, women's and men's backpacks, men's sling bags, laptop bags, women's purses, men's wallets, card-holder wallets, men's, automatic and women's belts, men's and women's watches, sunglasses, bracelets, gift sets for him and her, mug and notebook gift sets |
| **Local** | Stock bought from South African wholesalers, any brand | 10: power banks, phone chargers, Bluetooth earphones and earbuds, Bluetooth speakers, smart watches, wireless mice, flash drives, memory cards, gaming headsets |

A search result becomes a **candidate** only if all of these hold:

| Rule | Applies to | Default |
|---|---|---|
| Price between `MinPrice` and `MaxPrice` (inclusive) | all | R200 – R3,000 |
| Review count ≥ `MinReviewCount` | all | 20 |
| In stock (if `RequireInStock`) | all | required |
| **Not an established brand** | Import | Brand is empty, a generic name ("Generic", "Unbranded", ...) or a small private label not in the profile's `ExcludedBrands` |
| **No excluded title words** | Import | Title has none of the profile's `ExcludedTitleKeywords` |

Each Import search names a **filter profile** (`"Profile": "Fashion"`; default `Electronics`). Profiles live under `Discovery:ImportProfiles`, so each product family gets its own brand list and keywords. A search naming a profile that doesn't exist is skipped and logged as an error.

The price ceiling is also how FlipZA stays with **light** items: Takealot rarely lists product weight, so there is no weight filter.

#### Why the import filters exist

- **Brands.** Most generic listings on Takealot have no brand at all. Established brands (Belkin, Anker, Volkano, SanDisk, ...) bought from overseas marketplaces risk being counterfeit, and Takealot removes counterfeit listings. Small private labels (often importers' own brand names) are allowed, because that is the model you would follow; set `UnbrandedOnly` to `true` to exclude them too.
- **Brands are only checked in the brand field, never the title.** Generic accessories often say "for Apple Watch" or "Samsung compatible" in the title.
- **Costly-to-import keywords.** Titles containing these are skipped:
  - *bluetooth, wireless, wifi, smart*: radio equipment generally needs **ICASA** type approval;
  - *battery, rechargeable, mah, power bank, solar*: **lithium batteries** are dangerous goods for air freight;
  - *wall charger, car charger, charger plug*: mains chargers generally need an **NRCS** Letter of Authority.

  Keywords match whole words, ignoring case: `smart` matches "Smart Plug" but not "Smartphone Holder"; `mah` still matches "10000mAh". "Charger Cable" is **not** excluded, because only the specific charger phrases above are listed.
- **Limitation:** the keyword check reads only the product title in search results. A product whose title doesn't mention its battery or Bluetooth can still slip through; check its attributes before ordering.

#### The Fashion profile

- **Brands:** about 50 fashion, luggage and watch brands (Gucci, Michael Kors, Guess, Polo, Samsonite, Casio, Ray-Ban, ...). Look-alikes of these are counterfeits: SA customs seizes counterfeit goods and Takealot removes the listings.
- **Keywords:** `replica`, `inspired`, `dupe` and `1:1` (counterfeit signals), plus `smart` and `bluetooth` so smartwatches stay out of the fashion watch searches.
- **Fashion-specific costs not yet modelled:** leather goods and bags generally carry **higher customs duties** than electronics accessories, and bags are bulky, so Takealot's size-based fulfilment fees will be higher than the calculator's flat R38. Treat `MaxBuyPrice` for bags as optimistic.
- **What the first live run showed:** bags, backpacks, wallets, men's watches and gift sets produce plenty of candidates; belts, bracelets, women's watches and notebook gift sets produce few, because most are under R200 or have fewer than 20 reviews.

Local searches skip the brand and keyword filters: products bought from SA wholesalers are already compliant and genuine.

The R200 floor sits just above the lowest price at which any deal can work (about R176 with default settings; see [Deal calculator](deal-calculator.md#the-minimum-viable-selling-price)).

### Step 3: pick the tracked set
Candidates are ranked by review count within each search. Ranking all searches together would let a few high-review categories (for example Bluetooth headphones with thousands of reviews) take every slot, so the tracked set is chosen in two passes:

1. **Equal share.** Each search gets `max(1, MaxTrackedProducts ÷ number of searches)` slots, filled with its most-reviewed candidates. With 245 slots and 49 searches, that is 5 per search.
2. **Top-up.** Remaining slots (any plus any a search could not fill) go to the most-reviewed remaining candidates across all searches.

A product found by two searches is counted once and gets the channel of the first search (in configuration order) that found it. Products listed in `ProductIds` are always tracked, in addition to the 245, with channel `Manual`.

The implementation is `TakealotDiscoveryJob.PickTrackedSet`, covered by tests in `tests/FlipZA.Tests/Engine/TakealotDiscoveryJobTests.cs`.

### Step 4: update the database
- Products in the new set are marked `IsTracked = true`, and their `SourcingChannel` (`Import`, `Local` or `Manual`) is recorded.
- Products **not** in the set are marked `IsTracked = false`, **but only if every search succeeded**. A failed search would otherwise make its products look like they had dropped out.
- Products not yet in the database get a full product-details fetch, are priced by the deal calculator and saved. Existing products wait for the next refresh.

---

## Refresh job: building price history

1. Load up to `MaxProductsPerRun` tracked products, **least recently refreshed first** (oldest `LastSeenAtUtc`).
2. For each, fetch product details.
   - **404 (delisted):** mark it untracked.
   - **Success:** run the deal calculator (`ListingPricer`), then save. The product row is updated and a new snapshot (with competing offers) is appended.
   - **Any other error:** log it and continue with the next product.
3. Log a summary: `Takealot refresh complete: 243/245 saved, 1 delisted, 1 failed`. If **every** product fails, a `Critical` log is written, which usually means Takealot changed its API or response format.

---

## What is captured

Everything comes from the product-details response. Fields that Takealot omits are stored as empty (`NULL`).

### Product (`MarketplaceProducts`), updated on every scrape

| Field | Source in Takealot JSON | Why it matters |
|---|---|---|
| `ExternalId` | `meta.identifier` (e.g. `PLID98945485`) | Takealot's product id |
| `VariantId` | `buybox.tsin` | Specific variant (colour/size) |
| `Title`, `Subtitle`, `Brand` | `title`, `core.subtitle`, `core.brand` | |
| `Barcode` | Attribute `barcode`, else `flixmedia.ean` | **Key for matching supplier stock** |
| `ModelNumber` | Attribute `model_number` | Secondary matching key |
| `Department`, `CategoryPath`, `LeafCategoryId` | `breadcrumbs` | Success fees are set per category |
| `Dimensions`, `Weight`, `Warranty` | Attributes containing "dimensions", "weight", "warranty" | Fulfilment fees depend on size (often missing) |
| `Attributes` | All `product_information` attributes, as JSON | Everything else (power, colour, what's in the box, ...) |
| `Url`, `ImageUrl` | `desktop_href`, first gallery image | |
| `LastRawPayload` | The full response | Re-parse later without re-scraping |

### Snapshot (`ListingSnapshots`), one per scrape

| Field | Meaning |
|---|---|
| `Price` / `ListPrice` | Current buy-box price / struck-through "was" price |
| `IsInStock`, `StockStatus` | Availability, e.g. "In stock", "Ships in 3 - 5 work days" |
| `IsLeadTime` | `true` when the buy-box offer ships from the seller rather than a Takealot warehouse |
| `DistributionCentres` | Warehouses holding stock, e.g. `CPT,DBN,JHB` |
| `IsMarketplaceSeller` | `true` when a third-party seller (not Takealot itself) holds the buy box |
| `BuyBoxSellerId`, `BuyBoxSellerName` | Who holds the buy box (`Takealot` for Takealot's own retail stock) |
| `BuyBoxSellerVatRegistered`, `BuyBoxSellerRating` | The buy-box seller's VAT status and seller score |
| `StarRating`, `ReviewCount` | Review count growth between snapshots indicates sales |
| `OtherOffersCount`, `LowestOtherOfferPrice` | How crowded the listing is and the cheapest competitor |
| `PromotionName` | Active promotion, e.g. "Pay Day Deals" |
| `EstimatedNetProceeds`, `MaxBuyPrice` | Deal economics; see [Deal calculator](deal-calculator.md) |

Each snapshot also stores every **competing offer** (`CompetingOffers`): seller, price, condition, VAT status and stock.

---

## Politeness and resilience

Every Takealot request passes through this pipeline, configured in `src/FlipZA.Infrastructure/DependencyInjection.cs`. Handlers run in this order:

```text
TakealotClient
  → RobotsTxtHandler      blocks any URL robots.txt disallows
  → Resilience handler    retries and timeouts
  → ThrottlingHandler     at least 2.5 s between requests to the same host
  → network
```

### robots.txt
- Fetched once per host and cached for **24 hours** (`RobotsTxtCache`).
- Rules are evaluated per RFC 9309: the group for the `FlipZA` user agent if one exists, otherwise `*`; `*` and `$` wildcards; the **longest matching rule wins**, and on a tie `Allow` wins.
- If robots.txt returns a **4xx** status, everything is allowed. Any other failure (5xx, wrong status) means **everything is disallowed** until the next fetch.
- A disallowed request throws `RobotsDisallowedException`; the job logs it and moves on.

What Takealot's API robots.txt allows: product details and searches. It disallows sponsored products, login, tracking (`collect`), order tracking and popups.

### Retries and timeouts (Microsoft.Extensions.Http.Resilience standard handler)
- Up to **3 retries** with exponential back-off on transient failures (5xx, 408, 429), honouring `Retry-After`.
- **30 s** per attempt, **3 min** total per request.
- A circuit breaker stops calling Takealot for a while if most recent requests fail.

### Throttling
`HostThrottle` is a singleton that enforces `MinRequestIntervalMs` (default **2,500 ms**) between any two requests to the same host, across both jobs and including retries and robots.txt fetches.

### Identification
Requests carry `User-Agent: FlipZA/0.1 (+https://github.com/simseza022/FlipZA)`. Takealot can see who is calling and how to get in touch.

---

## When Takealot changes something

| Symptom | Likely cause | Fix |
|---|---|---|
| Every request returns 404 | API version retired | Update `Takealot:ApiVersion` (check the version the website uses in browser dev tools) |
| Saves succeed but fields are empty | Response format changed | Compare `LastRawPayload` with the parser; update `TakealotParser` and the test fixtures |
| `RobotsDisallowedException` in logs | Takealot changed robots.txt | Respect it; adjust what is scraped |
| Many 403 / 429 responses | Rate limited or blocked | Raise `MinRequestIntervalMs`, reduce searches or tracked products |
