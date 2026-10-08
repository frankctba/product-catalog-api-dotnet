using Demo.Domain.Common.Services;
using Demo.Infrastructure.ExchangeRates;
using Demo.Infrastructure.Messaging;
using Demo.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Infrastructure;

public static class InfrastructureSetup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructurePersistence();
        services.AddInfrastructureExchangeRates(configuration);
        services.AddInfrastructureHangfire();

        // Singleton: stateless, holds nothing per request.
        services.AddSingleton<IMessagePublisher, FakeMessagePublisher>();

        return services;
    }
}
