using Demo.Domain.Common;
using Demo.Domain.Modules.Inventory;

namespace Demo.UnitTests.TestDoubles;

internal class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public FakeProductRepository(List<Product> products) => _products = products;

    public Task<Product?> GetProduct(string sku) => Task.FromResult(_products.SingleOrDefault(p => p.Sku == sku));

    public Task<PagedResult<Product>> GetProducts(int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = _products.OrderBy(p => p.Sku).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<Product>(items, page, pageSize, _products.Count));
    }
}
