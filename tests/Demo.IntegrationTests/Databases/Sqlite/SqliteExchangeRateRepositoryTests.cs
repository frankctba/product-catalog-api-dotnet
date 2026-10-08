using Demo.IntegrationTests.Infrastructure.Persistence;

namespace Demo.IntegrationTests.Databases.Sqlite;

/// <summary>Runs <see cref="ExchangeRateRepositoryTests{TDatabase}"/> against SQLite.</summary>
public class SqliteExchangeRateRepositoryTests : ExchangeRateRepositoryTests<SqliteTestDatabase>;
