using Demo.Domain.Common;

namespace Demo.Domain.Modules.Inventory
{
    public class Product
    {
        public Product(string sku, string name, decimal price)
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                throw new DomainException("A product SKU is required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException($"Product '{sku}' must have a name.");
            }

            if (price < 0)
            {
                throw new DomainException($"The price of product '{sku}' cannot be negative.");
            }

            Sku = sku;
            Name = name;
            Price = price;
        }

        public string Sku { get; private set; }
        public string Name { get; private set; }
        public decimal Price { get; private set; }
    }
}
