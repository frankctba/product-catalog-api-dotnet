using Demo.Domain.Common;
using Demo.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Persistence;

public class ProductRepository : IProductRepository
{
    private readonly DemoDbContext _dbContext;

    public ProductRepository(DemoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product?> GetProductAsync(string sku, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public async Task<PagedResult<Product>> GetProductsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.Products.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Sku)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(items, page, pageSize, totalCount);
    }
}
