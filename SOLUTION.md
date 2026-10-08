# Solution Overview

This page summarises what was delivered for the three tasks, the main decisions and their trade-offs, and what was deliberately left out. The full analysis, with every finding and its priority, is in [Task1_CodeAnalysis.md](Task1_CodeAnalysis.md).

## What was delivered

| Task | Result |
|---|---|
| **1. Code analysis** | 48 findings, each with a category, priority, driver and phase, plus a remediation roadmap. Scalability and a possible distributed architecture are covered as separate sections. |
| **2. Exchange-rate sync** | A Hangfire recurring job fetches USD-based rates for EUR, CAD, GBP and CHF from Open Exchange Rates every Monday at 06:00 UTC. Each run appends a snapshot to the history and updates the latest rates in one transaction. |
| **3. Catalog with conversion** | `GET /api/products?currency=EUR&page=1&pageSize=50` returns every price in the requested currency, using the latest stored rate, with the currency in each item. USD when no currency is given. |

## Approach: Strategy 2

Two strategies were compared: implement every Task 1 finding, or implement what Tasks 2 and 3 need plus the most important findings and leave the rest documented. The second was chosen. Each change maps to a requirement or a visible gap, and the diff stays reviewable. The roadmap in Task1_CodeAnalysis.md shows which findings were addressed and which remain in the backlog.

**Scope limit: a single instance.** The app still uses SQLite and in-memory Hangfire storage. Running several instances needs the "scale-out pack" described in the roadmap: a shared database, shared Hangfire storage, migrations as a deployment step and a cache with an expiry.

## How to run and test

See the [README](README.md) for setup. In short:

```bash
cd src/Demo.Api
dotnet user-secrets set "OpenExchangeRates:AppId" "<your-app-id>"   # optional
dotnet run --launch-profile http
```

- Swagger: http://localhost:5022/swagger. Hangfire: http://localhost:5022/hangfire.
- Without an App ID the API still runs, and conversions return 503 until rates are stored.
- `dotnet test src/DemoSolution.slnx` runs 74 tests. CI runs the same tests on every push and fails on vulnerable packages.

## Architecture

| Layer | Contains |
|---|---|
| **Domain** | `Product`, `ExchangeRateSnapshot`, `ExchangeRate`, `LatestExchangeRate` with their invariants; repository interfaces |
| **Application** | `InventoryModule` (catalog and conversion), `ExchangeRateSyncModule` (sync use case), `CurrencyConverter`, `IExchangeRateProvider` port, options |
| **Infrastructure** | EF Core (SQLite) with migrations, repositories, `OpenExchangeRatesClient`, Hangfire job and schedule |
| **Api** | `ProductsController`, exception-to-ProblemDetails mapping, composition root |

`Program.cs` only composes the layers (`AddApplication()`, `AddInfrastructure()`) and configures the pipeline.

## Key decisions

| Decision | Why | Trade-off |
|---|---|---|
| **History plus a latest-rates table** | Task 2 asks for both. Task 3 then reads a small table by key instead of searching the history. | Two writes per sync, kept consistent by one transaction. |
| **Unique key on (provider, base, publication time)** | A retried or repeated sync stores nothing new. | A second sync within the provider's update interval is a no-op by design. |
| **"Latest only moves forward" lives in the domain** | `LatestExchangeRate.UpdateFrom` ignores an older publication arriving late. The rule is tested once, in the domain. | — |
| **Rates stored with 8 decimals** | Converted prices would be wrong with 2-decimal rates. | — |
| **Rounding: 2 decimals, half away from zero, once at the end** | All supported currencies use 2 decimals. Rounding intermediate values compounds errors. | Currencies with 0 or 3 decimals would need a per-currency rule. |
| **400 for an unsupported currency, 503 when no rate is stored** | 400 means the client made a mistake. 503 means the server cannot answer yet. | — |
| **Sync at startup when no rates exist** | Otherwise conversions fail until the first Monday. | One extra provider call on a fresh database. |
| **Permanent and transient failures are handled differently** | A rejected key or unexpected provider data fails at once (`ExchangeRateSyncException`). Network errors and 5xx are retried, first by the HTTP resilience pipeline and then by Hangfire. | — |
| **API key sent as a header, stored in user-secrets** | It never appears in source control or in logged URLs. | — |
| **Paged response `{ items, page, pageSize, totalCount }`** | Each response stays bounded as the catalog grows. | The Task 3 example shows a plain list. Each item still has exactly the requested shape. |
| **Base currency fixed to USD in configuration** | It is what the tasks require. The design for a configurable base currency per deployment is documented (CFG-2), but not built. | — |
| **No MediatR, AutoMapper or generic repository** | They would add indirection that this codebase does not need. | — |

## Tests

| Project | Covers |
|---|---|
| `Demo.UnitTests` (46) | Domain invariants, conversion and rounding, catalog logic and validation, sync use case, using in-memory test doubles |
| `Demo.IntegrationTests` (28) | Real repositories with the migrations applied, the HTTP client with a stub handler and its resilience pipeline, and the API end to end with `WebApplicationFactory` |

**The integration tests do not depend on a database provider.** Database tests passing on SQLite would prove nothing once production uses another database. So each suite is written once, against an `ITestDatabase` abstraction, and a small class per provider runs it:

```
tests/Demo.IntegrationTests/
  Infrastructure/Persistence/ExchangeRateRepositoryTests<TDatabase>   ← the tests, written once
  Infrastructure/Persistence/ProductRepositoryTests<TDatabase>
  Api/ProductsApiTests<TDatabase>        ← the API on whichever database the fixture provides
  Databases/ITestDatabase                ← "give me an empty real database"
  Databases/Sqlite/                      ← SqliteTestDatabase + one class per suite
```

The API tests replace the application's `DbContext` configuration with the test database, so they do not know the provider either. Moving to SQL Server means adding the provider, its migrations, `Databases/SqlServer/SqlServerTestDatabase` (for example with Testcontainers) and three one-line classes. The tests do not change, and the same suite then proves the behaviour on both databases.

## Known limitations

- **Single instance only.** See the scale-out pack in the roadmap.
- **Hangfire storage is in memory.** The schedule is registered again on every startup, but job history is lost on restart.
- **Dashboard access.** The Hangfire dashboard allows local requests only. A deployment needs authentication and a role-based filter (SEC-1, SEC-3).
- **Migrations and seeding run at startup.** This is fine for one instance, but it should be a deployment step once there are several (SCL-1).
- **Tests on SQLite only.** The integration tests do not cover a production database such as SQL Server yet (TST-2). They are written to make that a small step: see below.

## Commit history

The branch is split by theme to make review easier: the Task 2 and 3 implementation, then lifetimes and composition, test layout, clean code, domain invariants, HTTP resilience, integration tests, API documentation, and finally CI and this document.
