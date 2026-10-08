using Demo.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Demo.IntegrationTests.Infrastructure.Persistence;

/// <summary>
/// An in-memory SQLite database with the real migrations applied. It lives as long as the connection stays open.
/// Each test gets a fresh database; use a new context per step so reads hit the database, not the change tracker.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DemoDbContext> _options;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<DemoDbContext>().UseSqlite(_connection).Options;

        using var context = CreateContext();
        context.Database.Migrate();
    }

    public DemoDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
