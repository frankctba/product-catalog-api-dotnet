using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Demo.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core tools (dotnet ef migrations ...), so migrations can be created without starting the API.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DemoDbContext>
{
    public DemoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DemoDbContext>()
            .UseSqlite("Data Source=demo.db")
            .Options;

        return new DemoDbContext(options);
    }
}
