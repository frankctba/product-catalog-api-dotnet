using Microsoft.EntityFrameworkCore;

namespace Demo.IntegrationTests.Databases;

/// <summary>
/// A real, empty database for integration tests. Tests only depend on this interface, so the same suite
/// runs against any provider: adding a provider means adding an implementation and one small class per suite,
/// not changing the tests.
/// </summary>
public interface ITestDatabase : IDisposable
{
    /// <summary>Points EF Core at this database (provider and connection).</summary>
    void Configure(DbContextOptionsBuilder options);
}
