using Demo.IntegrationTests.Infrastructure.Persistence;

namespace Demo.IntegrationTests.Databases.Sqlite;

/// <summary>Runs <see cref="ProductRepositoryTests{TDatabase}"/> against SQLite.</summary>
public class SqliteProductRepositoryTests : ProductRepositoryTests<SqliteTestDatabase>;
