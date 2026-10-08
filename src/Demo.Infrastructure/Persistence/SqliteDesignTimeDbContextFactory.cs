using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Demo.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core tools (dotnet ef migrations ...), so migrations can be created without starting the API.
/// </summary>
/// <remarks>
/// The provider chosen here decides which SQL the migrations are generated for: the migrations in
/// Persistence/Migrations are SQLite migrations. Moving to another database (finding PER-4) needs a
/// migration set for that provider and its own design-time factory, for example SqlServerDesignTimeDbContextFactory.
/// If this factory is left unchanged, new migrations are silently generated for SQLite again.
/// </remarks>
public class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DemoDbContext>
{
    public DemoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DemoDbContext>()
            .UseSqlite("Data Source=demo.db")
            .Options;

        return new DemoDbContext(options);
    }
}
