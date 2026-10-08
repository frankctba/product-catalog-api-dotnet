using Demo.Domain.Common;
using Demo.Domain.Modules.Inventory;

namespace Demo.UnitTests.Domain.Modules.Inventory;

public class ProductTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesProduct()
    {
        var product = new Product("SKU1", "Classic Leather Jacket", 103.30M);

        Assert.Equal("SKU1", product.Sku);
        Assert.Equal("Classic Leather Jacket", product.Name);
        Assert.Equal(103.30M, product.Price);
    }

    [Fact]
    public void Constructor_WithZeroPrice_IsAllowed()
    {
        Assert.Equal(0M, new Product("FREE", "Gift Card Sleeve", 0M).Price);
    }

    [Theory]
    [InlineData("", "Name", 1)]
    [InlineData(" ", "Name", 1)]
    [InlineData("SKU1", "", 1)]
    [InlineData("SKU1", "Name", -0.01)]
    public void Constructor_WithInvalidValues_Throws(string sku, string name, double price)
    {
        Assert.Throws<DomainException>(() => new Product(sku, name, (decimal)price));
    }
}
