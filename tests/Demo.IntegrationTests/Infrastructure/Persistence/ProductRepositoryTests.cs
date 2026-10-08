using Demo.Domain.Modules.Inventory;
using Demo.Infrastructure.Persistence;
using Demo.IntegrationTests.Databases;

namespace Demo.IntegrationTests.Infrastructure.Persistence;

/// <summary>
/// Written once, run against every provider: see the derived classes under Databases/.
/// </summary>
public abstract class ProductRepositoryTests<TDatabase> : IDisposable
    where TDatabase : ITestDatabase, new()
{
    private readonly TDatabase _database = new();

    protected ProductRepositoryTests()
    {
        _database.Migrate();

        using var context = _database.CreateContext();
        context.Products.AddRange(
            new Product("SKU3", "Canvas Sneakers", 59.99M),
            new Product("SKU1", "Classic Leather Jacket", 103.30M),
            new Product("SKU2", "Wool Overcoat", 102.20M));
        context.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetProductsAsync_ReturnsPageOrderedBySku_WithTotalCount()
    {
        await using var context = _database.CreateContext();
        var repository = new ProductRepository(context);

        var firstPage = await repository.GetProductsAsync(page: 1, pageSize: 2, CancellationToken.None);
        var secondPage = await repository.GetProductsAsync(page: 2, pageSize: 2, CancellationToken.None);

        Assert.Equal(["SKU1", "SKU2"], firstPage.Items.Select(p => p.Sku));
        Assert.Equal(["SKU3"], secondPage.Items.Select(p => p.Sku));
        Assert.Equal(3, firstPage.TotalCount);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsProduct_OrNullWhenUnknown()
    {
        await using var context = _database.CreateContext();
        var repository = new ProductRepository(context);

        var product = await repository.GetProductAsync("SKU1", CancellationToken.None);

        Assert.Equal(("Classic Leather Jacket", 103.30M), (product!.Name, product.Price));
        Assert.Null(await repository.GetProductAsync("NOPE", CancellationToken.None));
    }
}
