using System.ComponentModel.DataAnnotations;
using Demo.Application.Modules;
using Demo.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private const string Json = "application/json";
    private const string ProblemJson = "application/problem+json";

    private readonly IInventoryModule _inventoryModule;

    public ProductsController(
        IInventoryModule inventoryModule
    )
    {
        _inventoryModule = inventoryModule;
    }

    /// <summary>
    /// Lists the product catalog, optionally converted to another currency.
    /// </summary>
    /// <param name="currency">Target currency (EUR, CAD, GBP or CHF, case-insensitive). Prices are in USD when omitted.</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Products per page, from 1 to 100.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">The requested page, with every price in the returned currency.</response>
    /// <response code="400">The currency is not supported, or the paging values are out of range.</response>
    /// <response code="503">No exchange rate is stored yet for the requested currency.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK, Json)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, ProblemJson)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] string? currency,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await _inventoryModule.GetProductsAsync(currency, page, pageSize, cancellationToken);
    }

    /// <summary>
    /// Gets one product, with its price in USD.
    /// </summary>
    /// <param name="sku">The product SKU, e.g. SKU1.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">The product.</response>
    /// <response code="404">No product has this SKU.</response>
    [HttpGet("{sku}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK, Json)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<ProductDto>> GetProduct(string sku, CancellationToken cancellationToken)
    {
        var product = await _inventoryModule.GetProductAsync(sku, cancellationToken);

        return product is null ? NotFound() : product;
    }
}
