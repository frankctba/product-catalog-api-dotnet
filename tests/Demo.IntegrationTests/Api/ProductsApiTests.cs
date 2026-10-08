using System.Net;
using System.Net.Http.Json;
using Demo.Application.Modules;
using Demo.Domain.Common;
using Demo.IntegrationTests.Databases;
using Microsoft.AspNetCore.Mvc;

namespace Demo.IntegrationTests.Api;

/// <summary>
/// End-to-end tests through HTTP: routing, validation, the exception handler, the database and the seed data.
/// The tests share one API instance; only EUR and GBP rates are ever stored, so CHF always has no rate.
/// Written once, run against every provider: see the derived classes under Databases/.
/// </summary>
public abstract class ProductsApiTests<TDatabase> : IClassFixture<CatalogApiFactory<TDatabase>>
    where TDatabase : ITestDatabase, new()
{
    private static readonly DateTime Monday = new(2026, 9, 28, 5, 0, 0, DateTimeKind.Utc);

    private readonly CatalogApiFactory<TDatabase> _factory;
    private readonly HttpClient _client;

    protected ProductsApiTests(CatalogApiFactory<TDatabase> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_WithoutCurrency_ReturnsTheSeededCatalogInUsd()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("/api/products");

        Assert.Equal(3, page!.TotalCount);
        Assert.All(page.Items, p => Assert.Equal("USD", p.Currency));
        Assert.Equal(new ProductDto("SKU1", "Classic Leather Jacket", 103.30M, "USD"), page.Items[0]);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("eur")]
    public async Task GetProducts_WithSupportedCurrency_ConvertsWithTheLatestStoredRate(string currency)
    {
        await _factory.StoreRatesAsync(Monday, ("EUR", 0.9201M), ("GBP", 0.7905M));

        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>($"/api/products?currency={currency}");

        Assert.Equal([95.05M, 94.03M, 55.20M], page!.Items.Select(p => p.Price));
        Assert.All(page.Items, p => Assert.Equal("EUR", p.Currency));
    }

    [Fact]
    public async Task GetProducts_WithUnsupportedCurrency_Returns400ProblemDetails()
    {
        var response = await _client.GetAsync("/api/products?currency=JPY");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Unsupported currency", problem.Title);
        Assert.Contains("USD, EUR, CAD, GBP, CHF", problem.Detail);
    }

    [Fact]
    public async Task GetProducts_WithSupportedCurrencyButNoStoredRate_Returns503ProblemDetails()
    {
        var response = await _client.GetAsync("/api/products?currency=CHF");

        var problem = await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Equal("Exchange rate unavailable", problem.Title);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task GetProducts_WithInvalidPaging_Returns400(string query)
    {
        var response = await _client.GetAsync($"/api/products?{query}");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProducts_SecondPage_ReturnsRemainingProducts()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("/api/products?page=2&pageSize=2");

        Assert.Equal("SKU3", Assert.Single(page!.Items).Sku);
    }

    [Fact]
    public async Task GetProduct_KnownSku_ReturnsProductInBaseCurrency()
    {
        var product = await _client.GetFromJsonAsync<ProductDto>("/api/products/SKU2");

        Assert.Equal(new ProductDto("SKU2", "Wool Overcoat", 102.20M, "USD"), product);
    }

    [Fact]
    public async Task GetProduct_UnknownSku_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/products/NOPE");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    private static async Task<ProblemDetails> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal((int)expected, problem!.Status);
        return problem;
    }
}
