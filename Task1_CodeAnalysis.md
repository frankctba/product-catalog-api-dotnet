# Task 1: Code Analysis & Refactoring Identification

## Objective
Analyze the existing codebase, run and test the application, and identify areas that you believe should be improved, refactored, or fixed.

## Instructions
1. **Explore the Code:** Review the current structure, following the flow from the API layer down to the Domain and Infrastructure layers.
2. **Run the Application:** Start the application, interact with the Swagger UI, check the Hangfire dashboard, and observe the application's behavior.
3. **Identify Improvements:** Look for issues related to clean architecture principles, design patterns, error handling, performance, or overall code quality.
4. **Document Findings:** You **do not** need to implement these changes right now. Simply document your findings directly in this file.

---

## 📝 Your Findings

### How this analysis was done
- Read every file, following the flow `ProductController` → `InventoryModule` → `IProductRepository` / `ProductRepository` → SQLite.
- `dotnet build`: succeeds, 0 errors, 8 warnings (all NU1903, vulnerable packages).
- `dotnet test`: 1 of 1 passes, but the test body is empty.
- Ran the app: `GET /api/products/SKU1` returns 200, an unknown SKU returns **500**, and `/hangfire` returns 200 with no jobs.
- `dotnet list package --vulnerable --include-transitive` was used to confirm the vulnerable packages.

### Context and assumptions
The findings are judged against the current code and against what comes next:
- **Task 2:** fetch EUR, CAD, GBP and CHF rates (base USD) from Open Exchange Rates every Monday with Hangfire, and store both the latest rates and their history.
- **Task 3:** `GET /api/products?currency=XXX` returns the whole catalog, converted with the latest stored rate, with the currency in the payload.
- **Production database:** SQLite is used for simplicity. The assumption is that production will use a different relational database (for example SQL Server), so persistence must be swappable without touching Domain or Application.

### Priority legend
| Priority | Meaning |
|---|---|
| **P1 – Critical** | Wrong behaviour or a risk today, or it blocks Tasks 2 and 3. Fix before building on top. |
| **P2 – High** | Will cause problems while implementing Tasks 2 and 3, or blocks the move to a production database. |
| **P3 – Medium** | Quality, maintainability or robustness. Fix soon. |
| **P4 – Low** | Polish and nice-to-have. |

**Driver** says why the item matters: **Current** = a problem in the starter code as it is. **T2** / **T3** = becomes important because of Task 2 / Task 3. **Prod-DB** = needed to move to a production database. **Scale** = needed to run more than one instance. **Multi-region** = needed for deployments with different base currencies.

**Phase** says when the item is addressed in the recommended plan (Strategy 2, see [Remediation roadmap](#remediation-roadmap)): **Step 1–3** = implemented with Tasks 2 and 3. **Quick win** = cheap, done alongside. **Review hardening** = done before the review to close visible gaps. **Scale-out** = the next step, needed to run multiple instances. **Backlog** = documented, not implemented yet.

### Summary
| ID | Finding | Category | Priority | Driver | Phase |
|---|---|---|---|---|---|
| ERR-1 | Unknown SKU returns HTTP 500 | Error handling | P1 | Current | Quick win |
| ERR-2 | No global exception handling or ProblemDetails | Error handling | P1 | Current | Step 3 |
| PER-1 | `EnsureCreated` and no migrations, so new tables are never created | Persistence | P1 | T2, Prod-DB | Step 1 |
| PER-2 | Provider-specific `decimal(18,2)`, stored as TEXT in SQLite, 2-decimal precision | Persistence | P1 | T2, T3, Prod-DB | Step 1 |
| PER-3 | No data model for exchange rates (latest + history) | Persistence | P1 | T2, T3 | Step 2 |
| CFG-1 | Hardcoded connection string, no configuration or secrets for the external API | Configuration | P1 | T2, Prod-DB | Step 1 |
| BG-1 | Hangfire uses in-memory storage and has no jobs | Background processing | P1 | T2 | Step 2 |
| BG-2 | Cold start: no rates exist until the first Monday | Background processing | P1 | T2, T3 | Step 2 |
| API-1 | API returns the domain entity directly, no currency in the payload | API design | P1 | Current, T3 | Step 3 |
| SEC-1 | Hangfire dashboard has no explicit authorization | Security | P1 | Current | Review hardening (partial) |
| SEC-2 | Vulnerable transitive packages | Security | P1 | Current | Quick win |
| PER-4 | Provider choice and database setup live in `Program.cs` | Persistence | P2 | Prod-DB | Scale-out |
| PER-5 | `Sku` has no length or collation, so case sensitivity differs between providers | Persistence | P2 | Prod-DB | Scale-out |
| PER-6 | Repositories are read-only, with no transaction or unit-of-work design | Persistence | P2 | T2 | Step 2 |
| PER-7 | No "list all products" query, no paging | Persistence | P2 | T3 | Step 3 |
| BG-3 | Missed Monday runs and time zone not defined | Background processing | P2 | T2 | Backlog |
| BG-4 | Hangfire storage must move to a production store separately | Background processing | P2 | Prod-DB | Scale-out |
| INT-1 | No resilient HTTP client design for the external rates API | External integration | P2 | T2 | Step 2 |
| DOM-1 | No currency concept and no conversion service | Domain | P2 | T3 | Step 3 |
| ARC-1 | DI lifetimes are all Transient | Architecture | P2 | T2 | Quick win |
| TST-1 | No meaningful tests, and the test project cannot reach Infrastructure or the API | Testing | P2 | Current, T2, T3 | Steps 2–3 |
| CFG-2 | Base currency is an implicit assumption, not configuration | Configuration | P2 | T3, Multi-region | Backlog |
| SCL-1 | Migrations and seeding run at startup on every instance | Scalability | P2 | Scale | Scale-out |
| PER-8 | Tracked reads and `FirstAsync` | Persistence | P3 | Current, T3 | Step 3 |
| PER-9 | Timestamp type for rates (`DateTimeOffset` limitations in SQLite) | Persistence | P3 | T2, T3 | Step 2 |
| BG-5 | Hangfire options are not configurable | Background processing | P3 | T2 | Backlog |
| API-2 | No input validation on `sku` | API design | P3 | Current | Backlog |
| API-3 | `currency` parameter needs validation and a defined error | API design | P3 | T3 | Step 3 |
| API-4 | Endpoint has no response-type documentation | API design | P3 | Current, T3 | Review hardening |
| DOM-2 | Rounding policy for converted prices is undefined | Domain | P3 | T3 | Step 3 |
| DOM-3 | `InventoryModule` is a pass-through with an unused logger | Domain | P3 | Current | Step 3 |
| DOM-4 | Anemic `Product` entity, public setters, no invariants | Domain | P3 | Current | Review hardening |
| ARC-2 | Infrastructure references Application with no reason today | Architecture | P3 | Current | Backlog |
| ARC-3 | Application and Infrastructure registrations live in `Program.cs` | Architecture | P3 | Current | Quick win |
| SEC-3 | No authentication, CORS policy or rate limiting | Security | P3 | Current | Backlog |
| PERF-1 | No `CancellationToken` propagation | Performance | P3 | Current, T3 | Review hardening |
| PERF-2 | Latest rates are read from the database on every request | Performance | P3 | T3 | Scale-out |
| TST-2 | No integration tests against the target database | Testing | P3 | Prod-DB | Backlog |
| OBS-1 | No health checks, noisy EF logging, no correlation | Observability | P3 | Current | Scale-out |
| DEAD-1 | `IMessagePublisher` / `FakeMessagePublisher` are never used | Dead code | P3 | Current | Backlog |
| SCL-2 | In-process cache cannot be invalidated across instances | Scalability | P3 | Scale | Scale-out |
| SCL-3 | Rate limiting would apply per instance | Scalability | P3 | Scale | Backlog |
| SCL-4 | No forwarded-headers handling behind a load balancer | Scalability | P3 | Scale | Scale-out |
| PER-10 | Seed data is not meaningful | Persistence | P4 | Current | Review hardening |
| DOM-5 | `IMessagePublisher` placement in Domain | Domain | P4 | Current | Backlog |
| QLT-1 | Naming and file inconsistencies | Code quality | P4 | Current | Review hardening |
| BLD-1 | Redundant package reference and duplicated project settings | Build / tooling | P4 | Current | Quick win |
| SCL-5 | No DbContext pooling or shared output cache | Scalability | P4 | Scale | Backlog |

---

### 1. Error handling

**ERR-1 · Unknown SKU returns HTTP 500 (P1)**
- Where: `ProductRepository.GetProduct` uses `FirstAsync`, which throws `InvalidOperationException` when no row matches. The controller does not catch it.
- Observed: `GET /api/products/NOPE` returns 500.
- Why it matters: a normal "not found" case looks like a server fault. It pollutes logs and alerts, and clients cannot tell the difference.
- Direction: the repository returns `Product?`, the module signals "not found" (null or a Result type), and the controller returns 404.

**ERR-2 · No global exception handling (P1)**
- Where: `Program.cs` has no `UseExceptionHandler` / `IExceptionHandler` and no `AddProblemDetails`.
- Why it matters: unhandled errors return inconsistent bodies and can leak internals. Task 3 adds new failure cases (unsupported currency, no rates available) that need a consistent error format.
- Direction: one exception-handling middleware that returns RFC 7807 `ProblemDetails` with a trace id, and logs the exception once.

### 2. Persistence and database portability

**What is already well abstracted:** `IProductRepository` lives in Domain and is implemented in Infrastructure. `InventoryModule` depends only on the interface. Domain and Application have no EF Core or SQLite references, and `Product` has no mapping attributes (all mapping is in `DemoDbContext`). Swapping the database therefore does not touch the business layers. The items below are the places where SQLite still leaks, or where Tasks 2 and 3 need more than exists today.

**PER-1 · `EnsureCreated` and no migrations (P1)**
- Where: `Program.cs` calls `db.Database.EnsureCreated()`.
- Why it matters: `EnsureCreated` does nothing when `demo.db` already exists, so the exchange-rate table from Task 2 would **silently never be created** in an existing database. It also cannot evolve a schema in production, and migrations are generated per provider.
- Direction: use EF Core migrations (the `Microsoft.EntityFrameworkCore.Design` package is already referenced), kept in the Infrastructure assembly. When the production database is introduced, plan a migration set for that provider.

**PER-2 · Provider-specific decimal mapping and precision (P1)**
- Where: `DemoDbContext` maps `Price` with `HasColumnType("decimal(18,2)")`.
- Why it matters:
  - The column-type string is provider-specific. SQLite ignores it and stores the value as TEXT, so ordering, comparison and aggregation on prices are unreliable.
  - It is the existing convention. Copying it for exchange rates would round them to 2 decimals (0.9234 → 0.92), which makes every conversion in Task 3 wrong.
- Direction: use the provider-neutral `HasPrecision(...)`. Give rates enough precision (for example `HasPrecision(18, 8)`). In SQLite, add a value converter if prices must be sorted or aggregated in the database.

**PER-3 · No data model for exchange rates (P1)**
- Where: nothing exists yet. Task 2 requires that the latest rates are updated and also historically logged.
- Why it matters: this model decides how Task 3 finds "the latest available rate" and how the job behaves when it retries.
- Direction: design it explicitly before coding. For example, one history table (base, currency, rate, rate timestamp from the provider, fetched-at) with a unique key on (currency, rate timestamp), plus either a "latest" table or a query on the most recent row. Writes must be idempotent, so a retried job does not duplicate history.

**PER-4 · Provider choice and database setup in `Program.cs` (P2)**
- Where: `UseSqlite(...)`, the `DemoDbContext` registration and the seeding all live in the API composition root.
- Why it matters: changing the database means editing the API project.
- Direction: move this behind an `AddInfrastructurePersistence(configuration)` extension in Infrastructure, with the provider and connection string chosen by configuration.

**PER-5 · `Sku` has no length or collation (P2)**
- Where: `DemoDbContext` configures only the key for `Sku`.
- Why it matters: SQLite compares text case-sensitively by default (BINARY collation), while SQL Server's default collation is case-insensitive. A lookup of `sku1` behaves differently between the two. There is also no maximum length.
- Direction: decide the rule (case-sensitive or not), set the length and collation explicitly, or normalise SKUs in the domain.

**PER-6 · Read-only repositories, no transaction design (P2)**
- Where: `IProductRepository` has only `GetProduct`, and nothing exposes `SaveChanges` or a transaction.
- Why it matters: Task 2 must update the latest rates and append history as one atomic operation.
- Direction: add a unit-of-work (`IUnitOfWork.SaveChangesAsync`) or a single atomic repository method for the write.

**PER-7 · No "list all products" query (P2)**
- Where: `IProductRepository` and `InventoryModule` can only fetch by SKU.
- Why it matters: Task 3 lists the entire catalog. An unbounded list grows with the catalog.
- Direction: add a read method that supports paging (or at least a limit), and decide whether paging belongs in the Task 3 contract.

**PER-8 · Tracked reads and `FirstAsync` (P3)**
- Where: `ProductRepository.GetProduct` uses `FirstAsync(p => p.Sku == sku)`.
- Why it matters: change tracking is unnecessary for read-only queries, and matters more for the full-catalog read in Task 3. `Sku` is the key, so `SingleOrDefaultAsync` expresses intent better.
- Direction: `AsNoTracking()` with `SingleOrDefaultAsync` (or a projection straight to a DTO).

**PER-9 · Timestamp type for rates (P3)**
- Why it matters: "latest available rate" means ordering by time. EF Core's SQLite provider has known limits when sorting and comparing `DateTimeOffset`.
- Direction: store UTC `DateTime` values (or Unix timestamps, which Open Exchange Rates already returns), and keep the type portable to the production database.

**PER-10 · Seed data is not meaningful (P4)**
- Where: both seeded products have `Name = "Name"`. Seeding runs in every environment.
- Direction: realistic data, seeded only in Development through a dedicated seeder.

### 3. Configuration and secrets

**CFG-1 · Hardcoded configuration, no secret handling (P1)**
- Where: `UseSqlite("Data Source=demo.db")` in `Program.cs`. `appsettings.json` contains only logging settings.
- Why it matters:
  - The database file location depends on the working directory, and the provider cannot change per environment.
  - Task 2 needs an API key (a secret), the API base URL and the schedule. None of these has a home yet. The key must never be committed.
- Direction: connection strings and options from configuration with typed options (`IOptions<T>`) validated at startup. Secrets from user-secrets locally and environment variables or a vault in production.

**CFG-2 · Base currency is an implicit assumption (P2)**
- Where: `Product.Price` is a bare `decimal`. "USD" exists only in the task descriptions, not in code, data or configuration.
- Why it matters: if the catalog is deployed in several regions with different base currencies (for example USD in the US, EUR in Europe), there is no single setting to change, and nothing stops a deployment from serving prices entered in another currency.
- Direction: a `BaseCurrency` setting per deployment, a currency code stored with each price, and a startup check that they match. Rates can still be stored against the provider's base (USD on the free Open Exchange Rates plan) and converted with a cross rate: `rate(P→T) = r(B→T) / r(B→P)`. When the provider base equals the deployment base (a paid plan, or a USD deployment), this reduces to a multiplication.

### 4. Background processing (Hangfire)

**BG-1 · In-memory storage and no jobs (P1)**
- Where: `HangfireSetup.cs` calls `UseMemoryStorage()`. No code enqueues or schedules a job.
- Why it matters: the server and dashboard run but do nothing. With memory storage, every job, retry and history entry is lost on restart.
- Direction: register the Task 2 job with `IRecurringJobManager` at startup (`Cron.Weekly(DayOfWeek.Monday)`). Use persistent storage, and make the job idempotent with `AutomaticRetry` and `DisableConcurrentExecution`.

**BG-2 · Cold start: no rates until the first Monday (P1)**
- Why it matters: the job runs only on Mondays. On a fresh database, Task 3 has no rates to convert with until the following Monday.
- Direction: trigger one sync on startup when no rates exist (or allow a manual trigger from the dashboard). Define what Task 3 returns when no rate is available, for example 503 with ProblemDetails.

**BG-3 · Missed runs and time zone (P2)**
- Why it matters: if the app is down on Monday, that run is skipped. "Monday" depends on the time zone of the server.
- Direction: set the recurring job time zone explicitly (UTC). On startup, check whether the latest stored rates are older than the last scheduled run, and catch up if so.

**BG-4 · Hangfire storage for production (P2)**
- Why it matters: Hangfire storage is separate from the application database. Moving the app to SQL Server does not move Hangfire.
- Direction: plan it as its own change (for example `Hangfire.SqlServer`), configured from settings, either in the app database under its own schema or in a separate one.

**BG-5 · Hangfire options are not configurable (P3)**
- Where: `AddInfrastructureHangfire()` has no options (queues, workers, retries, polling interval, cron).
- Direction: bind them from configuration so they can differ per environment.

### 5. External integration

**INT-1 · No resilient HTTP client design (P2)**
- Why it matters: Task 2 calls a third-party API with rate limits (the free plan has a monthly quota) and the usual network failures.
- Direction: put a typed `HttpClient` behind an interface (for example `IExchangeRateProvider`) in Infrastructure, so the provider can be swapped and mocked. Configure timeouts and retries (`Microsoft.Extensions.Http.Resilience`). Validate the response, for example that all four currencies are present and the base is USD. Log failures without logging the API key.

### 6. API design

**API-1 · Domain entity returned directly (P1)**
- Where: `ProductController.GetProduct` returns `Ok(product)` with a `Product` from Domain.
- Why it matters: any change to the entity changes the public contract. Task 3 requires the payload to include `currency`, which `Product` does not have.
- Direction: response DTOs (for example `ProductResponse { Sku, Name, Price, Currency }`), mapped in the Application layer.

**API-2 · No input validation on `sku` (P3)**
- Direction: validate length and format, and return 400 with ProblemDetails.

**API-3 · `currency` parameter validation (P3)**
- Why it matters: Task 3 supports only EUR, CAD, GBP and CHF (and USD as the default). The behaviour for `eur`, `XYZ` or an empty value is undefined.
- Direction: case-insensitive validation against the supported list, and 400 with a clear message otherwise. Keep the supported list in configuration, shared with the Task 2 job.

**API-4 · No response-type documentation (P3)**
- Where: no `[ProducesResponseType]` attributes, so Swagger shows only a generic 200.
- Direction: document 200, 400 and 404 (and 503 if BG-2 uses it), so the contract matches the real behaviour.

### 7. Domain and application logic

**DOM-1 · No currency concept, no conversion service (P2)**
- Where: `Product.Price` is a bare `decimal`.
- Why it matters: Task 3 converts prices. Spreading that logic across the controller or repository would be hard to test.
- Direction: the base currency is fixed (USD), so a full `Money` type is optional. At minimum, add a currency code type (or enum) and a conversion service in Application, `Convert(amount, rate)`, that is pure and unit-tested.

**DOM-2 · Rounding policy is undefined (P3)**
- Why it matters: converted prices like 95.4987 need a defined rule, and different rules give different prices.
- Direction: decide decimals per currency and the `MidpointRounding` mode, round once at the end, and test it.

**DOM-3 · `InventoryModule` is a pass-through (P3)**
- Where: `InventoryModule.GetProduct` only forwards to the repository. `_logger` is assigned and never used, and it is typed as non-generic `ILogger`.
- Direction: this is where the use-case logic for Tasks 2 and 3 belongs (not-found handling, mapping, conversion). Use `ILogger<InventoryModule>`, or remove it.

**DOM-4 · Anemic entity (P3)**
- Where: `Product` has public setters and no invariants. Nothing prevents a negative price or an empty name.
- Direction: private setters and a constructor or factory that enforces invariants. Apply the same to the new exchange-rate entity (a rate must be positive).

**DOM-5 · `IMessagePublisher` in Domain (P4)**
- Direction: messaging ports usually belong in Application. Confirm the intended placement.

### 8. Architecture and dependency injection

**ARC-1 · DI lifetimes are all Transient (P2)**
- Where: `Program.cs` registers the repository, publisher and module as Transient.
- Why it matters: it works today because `AddDbContext` is Scoped. Hangfire resolves each job in its own scope, so the Task 2 job and its repositories must use scoped lifetimes consistently, or the DbContext lifetime becomes unclear.
- Direction: Scoped for anything that depends on the DbContext, and Singleton for stateless services.

**ARC-2 · Infrastructure references Application with no reason today (P3)**
- Where: `Demo.Infrastructure.csproj` references `Demo.Application`, and gets `Demo.Domain` only transitively.
- Why it matters: the persistence code should depend only on Domain. The reference becomes legitimate once Hangfire job classes in Infrastructure call Application use cases (Task 2). Today nothing uses it.
- Direction: reference `Demo.Domain` directly. Keep the Application reference only for the job classes, or host the job classes in Application and keep Infrastructure on Domain.

**ARC-3 · Registrations in `Program.cs` (P3)**
- Direction: `AddApplication()` and `AddInfrastructure(configuration)` extension methods, in line with the existing `AddInfrastructureHangfire()`.

### 9. Security

**SEC-1 · Hangfire dashboard has no explicit authorization (P1)**
- Where: `app.UseHangfireDashboard()` is called with no options.
- Why it matters: by default the dashboard allows only local requests. That is safe on a laptop but easy to break behind a reverse proxy or in a container, and nothing in the code states the intent. The dashboard can trigger, delete and requeue jobs, including the Task 2 sync.
- Direction: add an explicit authorization filter (role or policy based) and enable the dashboard only where intended.

**SEC-2 · Vulnerable transitive packages (P1)**
All three are rated High and arrive transitively.

| Package | Version | CVE | Impact | Pulled in by |
|---|---|---|---|---|
| Microsoft.OpenApi | 2.4.1 | CVE-2026-49451 | DoS (stack overflow on circular schema references) | Swashbuckle.AspNetCore |
| Newtonsoft.Json | 11.0.1 | CVE-2024-21907 | DoS (deeply nested JSON) | Hangfire.Core |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.11 | CVE-2025-6965 | Memory corruption in bundled SQLite (no patched package yet) | EF Core SQLite |

- Real exposure today is low: Swagger only generates its own document, Hangfire serializes job arguments rather than request bodies, and EF Core parameterizes queries.
- Direction: pin `Newtonsoft.Json` ≥ 13.0.1 and `Microsoft.OpenApi` ≥ 2.7.5 with direct references. Accept or suppress the SQLite warning with a documented reason until a patch exists. It goes away once production moves off SQLite. Add a CI step that fails on new high-severity advisories.

**SEC-3 · No authentication, CORS or rate limiting (P3)**
- Direction: decide the intended access model, and add authentication and authorization, an explicit CORS policy and rate limiting before exposing the API.

### 10. Performance

**PERF-1 · No `CancellationToken` propagation (P3)**
- Where: the controller, module and repository do not accept or pass a `CancellationToken`.
- Why it matters: if the client disconnects, the query keeps running. This matters more for the full-catalog read in Task 3.
- Direction: accept `CancellationToken` in actions and pass it through every layer, including the Task 2 HTTP call.

**PERF-2 · Latest rates read on every request (P3)**
- Why it matters: the rates change once a week, but Task 3 would query them on every catalog request.
- Direction: cache the latest rates (`IMemoryCache` / `HybridCache`) with an expiry, or invalidate the cache when the Task 2 job stores new rates.

### 11. Testing

**TST-1 · No meaningful tests (P2)**
- Where: `UnitTest1.Test1` is empty. It passes, which gives false confidence. `Demo.UnitTests` references only `Demo.Application`, so it cannot test the repository, the API or the Hangfire setup.
- Direction:
  - Unit tests for the conversion and rounding logic (the most valuable tests in this assessment) and for `InventoryModule`, with mocked repositories.
  - Tests for the Task 2 job, with a fake `IExchangeRateProvider`: idempotency, partial or failed responses.
  - API tests with `WebApplicationFactory`: 200, 400 and 404, `?currency=` cases.
  - Delete the placeholder test.

**TST-2 · No integration tests against the target database (P3)**
- Why it matters: SQLite tests do not prove the behaviour of the production database (collation, decimals, dates).
- Direction: integration tests against the real target, for example SQL Server with Testcontainers, once that database is chosen.
- Prepared: the integration tests are written once against an `ITestDatabase` abstraction, and a small class per suite picks the provider (today only SQLite). Adding the production database means adding its provider, migrations and an `ITestDatabase` implementation; the tests themselves do not change.

### 12. Observability

**OBS-1 · Observability gaps (P3)**
- No health-check endpoint (`/health`). It would be useful to cover the database, Hangfire and how old the latest rates are.
- `appsettings.json` uses `Information` as the default level, so EF Core prints every SQL command in the console. Raise `Microsoft.EntityFrameworkCore` to `Warning`.
- No structured logging sink, no correlation or trace id, and no metrics. `InventoryModule` and the repository log nothing.

### 13. Dead code

**DEAD-1 · Messaging abstraction is never used (P3)**
- Where: `IMessagePublisher` and `FakeMessagePublisher` are registered but nothing injects them. The fake only logs a warning.
- Direction: remove them, or use them deliberately, for example to publish an `ExchangeRatesUpdated` event after Task 2 (which could also invalidate the PERF-2 cache). If kept, document that the fake is a placeholder.

### 14. Code quality

**QLT-1 · Naming and file inconsistencies (P4)**
- File `ProductsController.cs` contains class `ProductController`, and the namespace is `Ecommerce.Api.Controllers` while everything else is `Demo.*`.
- Async methods (`GetProduct`) do not use the `Async` suffix, although `IMessagePublisher.PublishAsync` does.
- Mixed file-scoped and block-scoped namespaces, and some files start with a UTF-8 BOM.
- Direction: agree on conventions and enforce them with an `.editorconfig`.

### 15. Build and tooling

**BLD-1 · Redundant reference and duplicated settings (P4)**
- `Demo.Api.csproj` references both `Microsoft.AspNetCore.OpenApi` and `Swashbuckle.AspNetCore`, but only Swashbuckle is used (`AddSwaggerGen`).
- `TargetFramework`, `Nullable` and `ImplicitUsings` are repeated in every `.csproj`.
- Direction: remove the unused package. Use `Directory.Build.props` and central package management (`Directory.Packages.props`). Consider `TreatWarningsAsErrors` once warnings are clean.

---

### 16. Scalability and multiple instances

The API itself is stateless and never calls the exchange-rate provider on the request path, which is the hard part to get right. What prevents running more than one instance is the state kept on each machine: a SQLite file per instance (PER-4), in-memory Hangfire storage per instance (BG-4), and work done at startup. The items below are the gaps that remain once those two are fixed.

**SCL-1 · Migrations and seeding at startup (P2)**
- Where: `Program.cs` creates the schema and seeds data on every start.
- Why it matters: with N instances starting together, they race to migrate and to insert the same seed rows. One can fail with a primary-key violation.
- Direction: run migrations and seeding as a separate deployment step (an EF Core migrations bundle or a one-off job).

**SCL-2 · Cache invalidation across instances (P3)**
- Why it matters: PERF-2 suggests clearing the rates cache when the sync job stores new rates. Only the instance that ran the job would clear its own cache, and the others would keep serving old rates.
- Direction: a short expiry time (rates change weekly, so an hour of staleness is acceptable), a shared cache (`HybridCache` / Redis), or an "exchange rates updated" event.

**SCL-3 · Rate limiting per instance (P3)**
- Why it matters: limits configured in the app (SEC-3) apply per instance, so N instances allow N times the limit.
- Direction: enforce limits at the gateway, or use a shared store.

**SCL-4 · No forwarded-headers handling (P3)**
- Why it matters: behind a load balancer, `UseHttpsRedirection` and client IPs see the proxy, not the client.
- Direction: `UseForwardedHeaders` with the known proxies configured, and health checks (OBS-1) so the load balancer can tell when an instance is ready.

**SCL-5 · No connection pooling or shared output cache (P4)**
- Direction: `AddDbContextPool`, and output caching per `currency` value (backed by Redis when there are several instances), with HTTP caching headers. The catalog changes rarely, so the response is very cacheable.

### 17. Future: distributed architecture

Not findings against the current code, but the items a split into separate services would need. They are only worth it with a concrete reason: many regions sharing one provider quota, one consistent rate set worldwide, or a team boundary.
- **DST-1:** a real message bus with an outbox, so database writes and published events cannot get out of step. Today only the `IMessagePublisher` port and a fake exist.
- **DST-2:** a separate rates service that fetches once and publishes `ExchangeRatesUpdated`. Each regional catalog stores the rates it receives. The catalog's layering already allows this as an Infrastructure change.
- **DST-3:** distributed tracing (OpenTelemetry).
- **DST-4:** versioned API and event contracts.
- **DST-5:** an API gateway and service discovery.

---

### Remediation roadmap

Two strategies were considered:
- **Strategy 1: implement every finding.** The result is production-grade on multiple instances, but it is a large change across every layer, and the Task 2 and Task 3 work is hard to review among the refactoring.
- **Strategy 2: implement what Tasks 2 and 3 need, plus the most important items (recommended).** Each change maps to a requirement, and the rest stays documented here. It targets **a single instance**, which is stated explicitly. Interfaces, options and migrations are put in place, so later steps slot in without rework.

The roadmap follows Strategy 2.

| Step | Goal | Findings |
|---|---|---|
| **Step 1 · Minimal foundation** | What Task 2 cannot work without | PER-1 migrations, PER-2 provider-neutral precision, CFG-1 configuration and secrets |
| **Step 2 · Task 2** | Weekly exchange-rate sync | PER-3 rate tables, PER-9 UTC timestamps, PER-6 atomic save, INT-1 typed HTTP client behind an interface, BG-1 Monday job in UTC, BG-2 sync on startup when no rates exist, TST-1 sync tests |
| **Step 3 · Task 3** | Catalog with currency conversion | PER-7 listing with paging, PER-8 no-tracking reads, API-1 response DTO with currency, DOM-1 converter, DOM-2 rounding rule, API-3 currency validation, ERR-2 ProblemDetails for the new 400 and 503 responses, TST-1 converter and catalog tests |
| **Quick wins** | Cheap, visible fixes done alongside | ERR-1 404 for an unknown SKU, SEC-2 pinned package versions, BLD-1 unused package removed, ARC-1 Scoped/Singleton lifetimes, ARC-3 `AddApplication()` / `AddInfrastructure()` registration |
| **Review hardening** | Close the gaps a reviewer would see first | DOM-4 invariants in the entities (also for the new exchange-rate entities), QLT-1 naming and `Async` suffix, PERF-1 cancellation tokens on every path, API-4 Swagger response types, PER-10 realistic seed data, SEC-1 dashboard access rule made explicit, TST-1 integration tests for the real repositories, HTTP client and API |
| **Scale-out pack (next)** | The minimum to run multiple instances | PER-4 and PER-5 shared database, BG-4 shared Hangfire storage, SCL-1 deploy-time migrations and seeding, PERF-2 and SCL-2 cache with expiry, OBS-1 health checks, SCL-4 forwarded headers |
| **Backlog** | Production hardening and clean-up | SEC-1 (role-based filter), SEC-3, CFG-2, BG-3, BG-5, API-2, DOM-5, ARC-2, TST-2, DEAD-1, BLD-1 (shared build props), SCL-3, SCL-5 |
| **Future** | Distributed architecture, only when justified | DST-1 to DST-5 |

**Partial in Strategy 2:** PER-6 as a single `SaveChangesAsync` (no unit-of-work abstraction), BG-1 on in-memory storage (the schedule is registered again on every startup, so it survives restarts, but job history does not), ERR-2 only for the cases the API needs, and TST-2: the integration tests are provider-independent but run on SQLite only, not on a production database yet.

**P1 items only partly done:** SEC-1 (dashboard authorization). The dashboard now explicitly allows local requests only, which is safe for a single local instance. A role-based filter needs authentication (SEC-3) and must be in place before any deployment.

### Notes
- Task 3 says it depends on "Task 1 (Exchange Rate Synchronization)". This appears to mean **Task 2**.
