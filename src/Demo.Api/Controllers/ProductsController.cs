using System.ComponentModel.DataAnnotations;
using Demo.Application.Modules;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IInventoryModule _inventoryModule;

    public ProductController(
        IInventoryModule inventoryModule
    )
    {
        _inventoryModule = inventoryModule;
    }

    /// <summary>
    /// Lists the product catalog. Prices are in USD unless a supported currency (EUR, CAD, GBP, CHF) is requested.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? currency,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var products = await _inventoryModule.GetProducts(currency, page, pageSize, cancellationToken);

        return Ok(products);
    }

    [HttpGet("{sku}")]
    public async Task<IActionResult> GetProduct(string sku)
    {
        var product = await _inventoryModule.GetProduct(sku);

        return product is null ? NotFound() : Ok(product);
    }
}
