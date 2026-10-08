using Demo.Application;
using Demo.Infrastructure;
using Demo.Infrastructure.ExchangeRates;
using Demo.Infrastructure.Persistence;
using Demo.Api;
using Hangfire;
using Hangfire.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml")));
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

// Add Hangfire Dashboard. Only local requests are allowed: there is no authentication yet (SEC-3),
// and the dashboard can trigger and delete jobs. A deployment needs a role-based filter instead.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new LocalRequestsOnlyAuthorizationFilter()]
});

app.MapControllers();

// Apply migrations and seed the database for testing
await app.Services.InitializeDatabaseAsync();

// Schedule the weekly exchange-rate sync (and run one now if no rates are stored yet)
await app.Services.ScheduleExchangeRateSyncAsync();

app.Run();

// Exposed so the integration tests can host the API with WebApplicationFactory<Program>.
public partial class Program;
