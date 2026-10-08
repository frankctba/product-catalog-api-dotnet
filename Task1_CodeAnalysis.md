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

Found by reading every file and running the original application: the build passed with 8 vulnerable-package warnings, the only test was empty, an unknown SKU returned **500**, and Hangfire was configured but had no jobs.

The changes needed for Tasks 2 and 3, plus the most important findings, were implemented. The rest are listed with their priority.

**Priority:** **P1** wrong behaviour or a risk today · **P2** blocks production use or scaling · **P3** quality and robustness · **P4** polish.

### Done

| ID | Finding | What was done |
|---|---|---|
| ERR-1 | Unknown SKU returned 500 | Returns 404 |
| ERR-2 | No global error handling | Errors return ProblemDetails (`application/problem+json`). Unexpected errors become a logged 500 without internal details. |
| PER-1 | `EnsureCreated`, no migrations: new tables would never be created in an existing database | EF Core migrations |
| PER-2 | Provider-specific `decimal(18,2)` mapping | `HasPrecision`, provider-neutral. Rates keep 8 decimals. |
| PER-3 | No model for exchange rates | History (snapshots and rates), plus a latest-rates table, written in one transaction. A unique key makes repeated syncs harmless. |
| PER-6 | No write or transaction design | One `SaveChanges` per sync |
| PER-7 | No "list products" query | Listing with paging (1–100 per page) |
| PER-8 | Reads were tracked, using `FirstAsync` | `AsNoTracking` and `SingleOrDefaultAsync` |
| PER-9 | Timestamp type for rates | UTC `DateTime` |
| PER-10 | Seed data was not meaningful | Three named products |
| CFG-1 | Hardcoded connection string, nowhere to keep the API key | Typed options, validated at startup. The API key comes from user-secrets or an environment variable. |
| BG-1 | Hangfire had no jobs | Recurring sync every Monday at 06:00 UTC. Failures that retrying won't fix are not retried. |
| BG-2 | No rates before the first Monday | Sync at startup when no rates are stored. Conversions return 503 until then. |
| INT-1 | No HTTP client design for the rates API | Typed client behind an interface, with retries, circuit breaker and timeouts. The key is sent as a header. |
| API-1 | Domain entity returned directly | Response DTO including the currency |
| API-3 | Currency parameter not validated | Case-insensitive. Unsupported currencies return 400 with the list of supported ones. |
| API-4 | Swagger only documented 200 | Every response code and the XML comments are documented |
| DOM-1 | No conversion logic | `CurrencyConverter` in the Application layer |
| DOM-2 | Rounding undefined | Rounded once, to 2 decimals, half away from zero |
| DOM-3 | `InventoryModule` was a pass-through with an unused logger | Holds the catalog and conversion logic |
| DOM-4 | Anemic entities, no invariants | Constructors or factories that enforce the rules, with private setters. "Latest rate only moves forward" is a domain rule. |
| ARC-1 | Everything registered as Transient | Scoped for anything using the DbContext, Singleton for stateless services |
| ARC-3 | All registrations in `Program.cs` | `AddApplication()` and `AddInfrastructure()` |
| SEC-2 | Vulnerable transitive packages | Patched versions pinned. CI fails on new vulnerable packages. |
| PERF-1 | No `CancellationToken` | Accepted and passed through on every path |
| TST-1 | One empty test | 46 unit tests and 28 integration tests (repositories, HTTP client, API). The integration tests do not depend on the database provider. |
| QLT-1 | Naming inconsistencies | `Demo.Api` namespace, `ProductsController`, `Async` suffix, one type per file |

### Not done yet

| ID | Finding | Priority | Suggested change |
|---|---|---|---|
| SEC-1 | Hangfire dashboard authorization | **P1** | Partly done: only local requests are allowed, explicitly. A deployment needs authentication (SEC-3) and a role-based filter. |
| PER-4 | Database provider fixed to SQLite | P2 | Choose the provider by configuration, and use a shared database server in production. The design-time factory (`SqliteDesignTimeDbContextFactory`) and the migrations are SQLite-specific: the new provider needs its own migration set and factory, or new migrations are silently generated for SQLite. |
| PER-5 | `Sku` has no length or collation: case sensitivity differs between databases | P2 | Decide whether `sku1` and `SKU1` are the same product, and set the length and collation explicitly |
| BG-3 | A Monday run is lost if the app is down | P2 | At startup, catch up when the latest rates are older than the last scheduled run |
| BG-4 | Hangfire storage is in memory | P2 | Persistent, shared storage. Job history is lost on restart, and with several instances the job would run once per instance. |
| CFG-2 | Base currency fixed to USD; prices carry no currency | P2 | Base currency per deployment, a currency stored with each price, cross rates |
| SCL-1 | Migrations and seeding run at startup | P2 | Run them as a deployment step before running several instances |
| BG-5 | Hangfire options (workers, queues) not configurable | P3 | Bind them from configuration |
| API-2 | `sku` not validated | P3 | Validate length and format, and return 400 |
| ARC-2 | Infrastructure gets the Domain project only through Application | P3 | Reference Domain directly |
| SEC-3 | No authentication, CORS policy or rate limiting | P3 | Add them before exposing the API |
| PERF-2 | Latest rates read from the database on every request | P3 | Cache with a short expiry, shared across instances (see SCL-2) |
| TST-2 | Integration tests run on SQLite only | P3 | Run the same tests against the production database, for example with Testcontainers. The tests are already independent of the provider. |
| OBS-1 | No health checks; EF Core logs every SQL command | P3 | `/health` endpoint (database, Hangfire, age of the rates) and a quieter EF log level |
| DEAD-1 | `IMessagePublisher` is never used | P3 | Remove it, or publish an "exchange rates updated" event |
| SCL-2 | An in-process cache can't be cleared on other instances | P3 | Short expiry, or a shared cache |
| SCL-3 | Rate limiting would apply per instance | P3 | Enforce limits at the gateway |
| SCL-4 | No forwarded headers behind a load balancer | P3 | `UseForwardedHeaders` |
| BLD-1 | Project settings repeated in every `.csproj` | P4 | `Directory.Build.props` and central package management. The unused OpenApi package was already removed. |
| DOM-5 | `IMessagePublisher` placed in Domain | P4 | Move it to Application |
| SCL-5 | No connection pooling or output caching | P4 | `AddDbContextPool`, and output caching per currency |

**Scope note:** the application targets a single instance. PER-4, BG-4, SCL-1, SCL-2 and SCL-4 are the minimum needed to run several instances.
