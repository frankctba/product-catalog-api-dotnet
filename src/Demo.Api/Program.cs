using Demo.Application;
using Demo.Infrastructure;
using Demo.Infrastructure.ExchangeRates;
using Demo.Infrastructure.Persistence;
using Ecommerce.Api;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Setup Application (modules, options) and Infrastructure (database, repositories, exchange rates, Hangfire)
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

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

// Apply migrations and seed the database for testing
await app.Services.InitializeDatabase();

// Schedule the weekly exchange-rate sync (and run one now if no rates are stored yet)
await app.Services.ScheduleExchangeRateSync();

app.Run();
