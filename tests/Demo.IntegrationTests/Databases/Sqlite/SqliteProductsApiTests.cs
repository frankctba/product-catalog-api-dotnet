using Demo.IntegrationTests.Api;

namespace Demo.IntegrationTests.Databases.Sqlite;

/// <summary>Runs <see cref="ProductsApiTests{TDatabase}"/> against SQLite.</summary>
public class SqliteProductsApiTests(CatalogApiFactory<SqliteTestDatabase> factory)
    : ProductsApiTests<SqliteTestDatabase>(factory);
