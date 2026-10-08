using AutoFno.Application.Interfaces;
using AutoFno.Application.Services;
using AutoFno.Infrastructure.BackgroundServices;
using AutoFno.Infrastructure.Data;
using AutoFno.Infrastructure.Data.Repositories;
using AutoFno.Infrastructure.Services;
using AutoFno.Infrastructure.Upstox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoFno.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddSingleton<SqlConnectionFactory>();
        services.AddScoped<IConnectionRepository, ConnectionRepository>();
        services.AddScoped<IInstrumentRepository, InstrumentRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IFnOStrategyRepository, FnOStrategyRepository>();

        // Upstox Configuration & Client
        services.Configure<UpstoxConfig>(configuration.GetSection("Upstox"));
        services.AddHttpClient<IUpstoxClient, UpstoxClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Application Services
        services.AddScoped<IUpstoxAuthService, UpstoxAuthService>();
        services.AddScoped<IFnOStrategyEngine, FnOStrategyEngine>();
        services.AddScoped<IFnORiskManager, FnORiskManager>();
        services.AddScoped<IFnOOrderService, FnOOrderService>();
        services.AddScoped<IPortfolioService, PortfolioService>();

        // Market Data Service registered as Scoped
        services.AddScoped<IFnOMarketDataService, FnOMarketDataService>();

        // Real-Time SignalR Notification Service
        services.AddSingleton<ITradingNotificationService, SignalRTradingNotificationService>();

        // Background Workers
        services.AddHostedService<FnOMarketFeedBackgroundService>();
        services.AddHostedService<FnOStrategyExecutionBackgroundService>();

        return services;
    }
}
