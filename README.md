# Technical Assessment

Welcome to the technical assessment! This repository contains a starter project designed for evaluating candidates.

## 📌 Project Overview
This project is a simplified backend API. It provides the foundation for you to demonstrate your system design skills, coding standards, understanding of clean architecture, and ability to tackle complex backend scenarios using modern .NET technologies.

## 🏗️ Architecture & Design
The codebase is structured following **Clean Architecture** and **Domain-Driven Design (DDD)** principles to separate concerns and ensure maintainability.

* **`Demo.Api`**: The entry point of the application. It contains ASP.NET Core Controllers, Swagger configuration, and Dependency Injection setups.
* **`Demo.Application`**: Contains the business orchestration logic (e.g., `InventoryModule`). This layer coordinates the workflow.
* **`Demo.Domain`**: The core of the system. It contains domain entities (like `Product`), interfaces (e.g., `IProductRepository`), and core business logic.
* **`Demo.Infrastructure`**: Implementation details. This layer handles data persistence using Entity Framework Core (SQLite), external services, and background job processing with Hangfire.
* **`tests/Demo.UnitTests`**: Unit tests for the domain and application logic (no database).
* **`tests/Demo.IntegrationTests`**: Integration tests for the repositories, the exchange-rate HTTP client and the API.

Application code lives in `src/` and tests in `tests/`. The solution file is `src/DemoSolution.slnx`.

## 🛠️ Tech Stack
* **Framework:** .NET 10.0
* **Persistence:** Entity Framework Core with SQLite (used for simplicity so no external database setup is required)
* **Background Processing:** Hangfire (configured with Memory Storage)
* **Testing:** xUnit

## 🚀 Getting Started

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* An IDE of your choice (Visual Studio 2022, JetBrains Rider, or VS Code)

### Running the Application

1. **Restore dependencies and build the solution** (from the root directory):
   ```bash
   dotnet build src/DemoSolution.slnx
   ```

2. **Run the API:**
   Navigate to the API folder and start the application. The SQLite database will be automatically created and seeded with initial data on startup.
   ```bash
   cd src/Demo.Api
   dotnet run
   ```

3. **Useful Endpoints:**
   * **Swagger UI:** `https://localhost:<port>/swagger` (or `http://localhost:<port>/swagger`) - Explore and test the API endpoints.
   * **Hangfire Dashboard:** `https://localhost:<port>/hangfire` - Monitor background jobs.

### Exchange Rates (Open Exchange Rates API key)
The weekly exchange-rate sync (every Monday, 06:00 UTC) needs an Open Exchange Rates App ID. Keep it out of source control with user-secrets:
```bash
cd src/Demo.Api
dotnet user-secrets set "OpenExchangeRates:AppId" "<your-app-id>"
```
When no rates are stored yet, a sync is enqueued at startup. You can also trigger it manually from the Hangfire dashboard (**Recurring Jobs → exchange-rate-sync → Trigger now**). Without an App ID the API still runs, but currency conversion returns `503` until rates are stored.

The database schema is managed with EF Core migrations and applied at startup. A `demo.db` created by an older version of the app (with `EnsureCreated`) is not compatible: delete it once and it is recreated.

### Running Tests
To execute the unit and integration tests, run the following command from the root directory:
```bash
dotnet test src/DemoSolution.slnx
```

---

## ✅ Solution
What was delivered, the main design decisions and the known limitations are summarised in **[SOLUTION.md](SOLUTION.md)**.

---

## 📝 Candidate Instructions

Your assessment is broken down into specific tasks. Please start by reading:

👉 **[Task 1: Code Analysis & Refactoring Identification](Task1_CodeAnalysis.md)**

👉 **[Task 2: Exchange Rate Synchronization](Task2_ExchangeRates.md)**

👉 **[Task 3: Product Catalog with Currency Conversion](Task3_ProductCatalogCurrency.md)**

Please complete the tasks assigned above. You are expected to treat this codebase as if it were a real production system.

**Note:** There is no single "correct" answer for these tasks. What matters most is your ability to explain your reasoning and justify the technical decisions you make. Additionally, there are no restrictions on the tools or resources you use to complete this assessment, feel free to use whatever helps you deliver your best work.

**Good luck!**