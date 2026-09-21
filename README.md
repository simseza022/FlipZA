# FlipZA — E-Commerce Deal Intelligence & Arbitrage Engine 

**FlipZA** is a high-performance South African e-commerce intelligence and arbitrage platform built with .NET 9. It automatically tracks, scans, and analyzes pricing spreads between wholesale suppliers (like Makro, Alibaba, and local distributors) and major South African marketplaces (Takealot and Makro) to surface high-ROI reselling opportunities.

---

### 🏗 System Architecture

FlipZA is built following **Clean Architecture** principles to keep core business rules decoupled from external dependencies and infrastructure.

```text
FlipZA/
 ├── src/
 │    ├── FlipZA.Core/            # Pure C# domain entities, interfaces, and margin formulas
 │    ├── FlipZA.Infrastructure/  # EF Core SQLite DbContext, HttpClient wrappers, Playwright scrapers
 │    ├── FlipZA.Engine/          # Background Worker Service running Quartz.NET & Telegram alerts
 │    └── FlipZA.Api/             # REST API & Webhook endpoints for user dashboards
 └── tests/
      └── FlipZA.Tests/           # Unit & Integration tests
```

### 🛠 Tech Stack
Framework: .NET 9.0 C#

Background Worker: .NET Worker Service (BackgroundService) + Quartz.NET

Web Scraping & API Parsing: HttpClient + System.Text.Json (Takealot JSON endpoints) & Microsoft.Playwright (Headless Chromium)

Database & Persistence: Entity Framework Core 9 + SQLite

Architecture Pattern: Clean Architecture (Domain-Driven Core)

Deployment: Docker & Docker Compose on Ubuntu Linux VPS
