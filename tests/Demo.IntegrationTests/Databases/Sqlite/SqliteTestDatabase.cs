using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Demo.IntegrationTests.Databases.Sqlite;

/// <summary>
/// A temporary SQLite file, the same kind of database the application uses. Deleted on dispose.
/// </summary>
public sealed class SqliteTestDatabase : ITestDatabase
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"demo-tests-{Guid.NewGuid():N}.db");

    public void Configure(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={_path}");

    public void Dispose()
    {
        // Pooled connections keep the file open.
        SqliteConnection.ClearAllPools();

        foreach (var file in new[] { _path, $"{_path}-wal", $"{_path}-shm" })
        {
            File.Delete(file);
        }
    }
}
