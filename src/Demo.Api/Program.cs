using Demo.Application.Common;
using Demo.Application.Modules;
using Demo.Domain.Common.Services;
using Demo.Domain.Modules.ExchangeRates;
using Demo.Domain.Modules.Inventory;
using Demo.Infrastructure;
using Demo.Infrastructure.ExchangeRates;
using Demo.Infrastructure.Messaging;
using Demo.Infrastructure.Persistence;
using Ecommerce.Api;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Setup Configuration
builder.Services.Configure<CurrencyOptions>(builder.Configuration.GetSection(CurrencyOptions.SectionName));

// Setup Database
builder.Services.AddDbContext<DemoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DemoDb")));

// Setup Repositories and Infra
builder.Services.AddTransient<IProductRepository, ProductRepository>();
builder.Services.AddTransient<IExchangeRateRepository, ExchangeRateRepository>();
builder.Services.AddTransient<IMessagePublisher, FakeMessagePublisher>();
builder.Services.AddInfrastructureExchangeRates(builder.Configuration);

// Setup Modules
builder.Services.AddTransient<IInventoryModule, InventoryModule>();
builder.Services.AddTransient<IExchangeRateSyncModule, ExchangeRateSyncModule>();

// Setup Hangfire via Infrastructure
builder.Services.AddInfrastructureHangfire();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Hangfire Dashboard
app.UseHangfireDashboard();

app.MapControllers();

// Seed Database for Testing
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
    db.Database.Migrate();

    if (!db.Products.Any())
    {
        db.Products.Add(
            new Product()
            {
                Sku = "SKU1",
                Name = "Name",
                Price = 103.30M
            }
        );

        db.Products.Add(
            new Product()
            {
                Sku = "SKU2",
                Name = "Name",
                Price = 102.20M
            }
        );

        db.SaveChanges();
    }
}

// Schedule the weekly exchange-rate sync (and run one now if no rates are stored yet)
await app.Services.ScheduleExchangeRateSync();

app.Run();
