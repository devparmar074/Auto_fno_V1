using AutoFno.Application.V2.Engines;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoFno.Infrastructure.V2;

public static class V2DependencyInjection
{
    public static IServiceCollection AddV2Services(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration with defaults
        var v2Config = new QuantitativeConvictionV2Config();
        var section = configuration.GetSection("QuantitativeConvictionV2");
        if (section.Exists())
        {
            section.Bind(v2Config);
        }
        services.AddSingleton(v2Config);

        // Calculation Engines (Stateless singletons)
        services.AddSingleton<IMarketDataValidationEngine, MarketDataValidationEngine>();
        services.AddSingleton<IPCRAnalysisEngine, PCRAnalysisEngine>();
        services.AddSingleton<IOIAnalysisEngine, OIAnalysisEngine>();
        services.AddSingleton<IGreeksAnalysisEngine, GreeksAnalysisEngine>();
        services.AddSingleton<ISpotTrendEngine, SpotTrendEngine>();
        services.AddSingleton<IConvictionScoringEngineV2, ConvictionScoringEngine>();
        services.AddSingleton<ITradeQualificationEngine, TradeQualificationEngine>();
        services.AddSingleton<IRiskManagementEngineV2, RiskManagementEngine>();
        services.AddSingleton<ITradeManagementEngineV2, TradeManagementEngine>();
        services.AddSingleton<IStrategyComparisonEngine, StrategyComparisonEngine>();

        // Stateful Singletons (Shadow trade tracking, metrics, lifecycle coordination)
        services.AddSingleton<IPerformanceAnalyticsEngine, PerformanceAnalyticsEngine>();
        services.AddSingleton<IConvictionSystemV2Coordinator, ConvictionSystemV2Coordinator>();

        return services;
    }
}
