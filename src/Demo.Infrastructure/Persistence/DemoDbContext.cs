using Demo.Domain.Modules.ExchangeRates;
using Demo.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infrastructure.Persistence;

public class DemoDbContext : DbContext
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots { get; set; } = null!;
    public DbSet<ExchangeRate> ExchangeRates { get; set; } = null!;
    public DbSet<LatestExchangeRate> LatestExchangeRates { get; set; } = null!;

    public DemoDbContext(DbContextOptions<DemoDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Sku);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ExchangeRateSnapshot>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Provider).IsRequired().HasMaxLength(50);
            b.Property(s => s.BaseCurrency).IsRequired().HasMaxLength(3).IsFixedLength();

            // Makes the sync idempotent: the same publication is never stored twice.
            b.HasIndex(s => new { s.Provider, s.BaseCurrency, s.RateTimestampUtc }).IsUnique();

            b.HasMany(s => s.Rates).WithOne().HasForeignKey(r => r.SnapshotId);
            b.Navigation(s => s.Rates).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ExchangeRate>(b =>
        {
            b.HasKey(r => new { r.SnapshotId, r.QuoteCurrency });
            b.Property(r => r.QuoteCurrency).HasMaxLength(3).IsFixedLength();
            b.Property(r => r.Rate).HasPrecision(18, 8);
        });

        modelBuilder.Entity<LatestExchangeRate>(b =>
        {
            b.HasKey(r => new { r.BaseCurrency, r.QuoteCurrency });
            b.Property(r => r.BaseCurrency).HasMaxLength(3).IsFixedLength();
            b.Property(r => r.QuoteCurrency).HasMaxLength(3).IsFixedLength();
            b.Property(r => r.Rate).HasPrecision(18, 8);
            b.HasOne(r => r.Snapshot).WithMany().HasForeignKey(r => r.SnapshotId);
        });
    }
}
