# Architecture Overview

A layered monolith following Clean Architecture, with two business capabilities: the **product catalog** (listing products, with prices converted to a requested currency) and **exchange rates** (a weekly sync from Open Exchange Rates, with history). Both capabilities share the layers and one database.

## Projects and dependencies

```mermaid
flowchart LR
    Api["Demo.Api<br/>controllers, ProblemDetails,<br/>composition root"]
    App["Demo.Application<br/>use cases, ports, options"]
    Dom["Demo.Domain<br/>entities, invariants,<br/>repository interfaces"]
    Inf["Demo.Infrastructure<br/>EF Core + SQLite, repositories,<br/>HTTP client, Hangfire job"]
    UT["tests/Demo.UnitTests"]
    IT["tests/Demo.IntegrationTests"]

    Api --> App
    Api --> Inf
    Inf --> App
    App --> Dom
    UT --> App
    IT --> Api
```

Dependencies point inwards: Domain depends on nothing, and Application only on Domain. Infrastructure implements the interfaces defined in Domain (repositories) and Application (`IExchangeRateProvider`). It references Application because the Hangfire job calls an Application use case. `Program.cs` only composes the layers with `AddApplication()` and `AddInfrastructure()` and configures the HTTP pipeline.

| Layer | Main types |
|---|---|
| **Domain** | `Product`, `ExchangeRateSnapshot`, `ExchangeRate`, `LatestExchangeRate`, `DomainException`, `CurrencyCodes`, `IProductRepository`, `IExchangeRateRepository`, `PagedResult<T>` |
| **Application** | `InventoryModule` (catalog queries and conversion), `ExchangeRateSyncModule` (sync use case), `CurrencyConverter`, `IExchangeRateProvider` and `ProviderRates`, `CurrencyOptions`, `ProductDto`, application exceptions |
| **Infrastructure** | `DemoDbContext` and migrations, `ProductRepository`, `ExchangeRateRepository`, `OpenExchangeRatesClient`, `ExchangeRateSyncJob`, Hangfire setup |
| **Api** | `ProductsController`, `ApiExceptionHandler` |

**Lifetimes:** repositories, modules and the job are Scoped, matching the `DbContext` (one per HTTP request or Hangfire job). The stateless message publisher is a Singleton. The typed `HttpClient` is Transient by design of `IHttpClientFactory`.

## Configuration

All settings are in `src/Demo.Api/appsettings.json`, bound to typed options and validated at startup. Any value can be overridden with an environment variable, using `__` in place of `:`.

| Section | Settings | Notes |
|---|---|---|
| `ConnectionStrings:DemoDb` | `Data Source=demo.db` | Relative to the folder the app is started from |
| `Currency` | `BaseCurrency` = `USD`, `SupportedCurrencies` = `EUR, CAD, GBP, CHF` | 3-letter uppercase codes; the base must not be in the list |
| `OpenExchangeRates` | `BaseUrl`, `AppId`, `TimeoutSeconds` = 10 | `AppId` is a secret: user-secrets locally, environment variables in production. Without it the API runs and conversions return 503. |
| `ExchangeRateSync` | `Cron` = `0 6 * * 1`, `SyncOnStartupWhenMissing` = `true` | The cron is evaluated in UTC (Mondays 06:00) |

## Catalog request

`GET /api/products?currency=EUR&page=1&pageSize=50`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctl as ProductsController
    participant M as InventoryModule
    participant ER as ExchangeRateRepository
    participant PR as ProductRepository

    C->>Ctl: GET /api/products?currency=eur
    Note over Ctl: page 1+, pageSize 1–100,<br/>otherwise 400
    Ctl->>M: GetProductsAsync("eur", page, pageSize)
    M->>M: normalise and validate the currency<br/>(unsupported → 400)
    M->>ER: GetLatestRatesAsync("USD")
    ER-->>M: latest USD rates<br/>(no EUR rate → 503)
    M->>PR: GetProductsAsync(page, pageSize)
    PR-->>M: products ordered by SKU + total count
    M->>M: CurrencyConverter.Convert(price, rate)<br/>round once, 2 decimals, half away from zero
    M-->>Ctl: PagedResult of ProductDto
    Ctl-->>C: 200 { items: [{ sku, name, price, currency }], page, pageSize, totalCount }
```

Without a currency, or with `USD`, prices are returned as stored and no rate is read. Errors become `application/problem+json` responses: the `ApiExceptionHandler` maps `UnsupportedCurrencyException` to 400 and `ExchangeRateUnavailableException` to 503, MVC returns 400 for invalid paging and 404 for an unknown SKU, and anything else becomes a generic 500 that is logged.

## Exchange-rate sync

```mermaid
sequenceDiagram
    participant HF as Hangfire
    participant Job as ExchangeRateSyncJob
    participant Mod as ExchangeRateSyncModule
    participant OXR as OpenExchangeRatesClient
    participant Dom as ExchangeRateSnapshot
    participant Repo as ExchangeRateRepository

    HF->>Job: Mondays 06:00 UTC,<br/>or at startup when no rates are stored
    Job->>Mod: SyncLatestRatesAsync
    Mod->>OXR: GetLatestRatesAsync("USD", [EUR, CAD, GBP, CHF])
    Note over OXR: GET latest.json?symbols=...<br/>Authorization: Token (App ID)<br/>retries 5xx / 408 / 429 / network errors
    OXR-->>Mod: ProviderRates
    Mod->>Mod: base is USD? every currency present?<br/>(no → ExchangeRateSyncException)
    Mod->>Dom: Create(...) keeps only the configured currencies<br/>rates > 0, ISO codes, UTC timestamps
    Mod->>Repo: SaveSnapshotAsync(snapshot)
    Note over Repo: already stored? → skip (idempotent)<br/>add snapshot + history rows<br/>LatestExchangeRate.Create / UpdateFrom<br/>one SaveChanges = one transaction
```

**Failures.** A failure that retrying will not fix is an `ExchangeRateSyncException`: no or rejected API key (4xx), an unexpected base, a missing currency, or a rate that breaks a domain invariant. The job logs it and fails immediately, without retries. Transient failures (network errors, 5xx, 408, 429) are first retried by the HTTP resilience pipeline. If they still fail, Hangfire retries the job up to 3 times. `DisableConcurrentExecution` prevents two syncs from running at once.

**Schedule.** The recurring job is registered again on every startup, so it survives restarts even though Hangfire storage is in memory. Job history does not survive a restart.

## What is not covered here

Running more than one instance and a configurable base currency are not implemented. See "Not done yet" in [Task1_CodeAnalysis.md](../../Task1_CodeAnalysis.md).
