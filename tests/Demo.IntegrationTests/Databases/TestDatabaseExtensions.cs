using Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demo.IntegrationTests.Databases;

public static class TestDatabaseExtensions
{
    /// <summary>
    /// A new context on the test database. Use a new one per step so reads hit the database, not the change tracker.
    /// </summary>
    public static DemoDbContext CreateContext(this ITestDatabase database)
    {
        var options = new DbContextOptionsBuilder<DemoDbContext>();
        database.Configure(options);

        return new DemoDbContext(options.Options);
    }

    /// <summary>Applies the application's real migrations.</summary>
    public static void Migrate(this ITestDatabase database)
    {
        using var context = database.CreateContext();
        context.Database.Migrate();
    }
}
