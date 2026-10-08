using Demo.Domain.Common;

namespace Demo.Domain.Modules.Inventory
{
    public interface IProductRepository
    {
        Task<Product?> GetProductAsync(string sku, CancellationToken cancellationToken);

        Task<PagedResult<Product>> GetProductsAsync(int page, int pageSize, CancellationToken cancellationToken);
    }
}
