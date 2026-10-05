using Demo.Domain.Common;

namespace Demo.Domain.Modules.Inventory
{
    public interface IProductRepository
    {
        Task<Product?> GetProduct(string sku);

        Task<PagedResult<Product>> GetProducts(int page, int pageSize, CancellationToken cancellationToken);
    }
}
