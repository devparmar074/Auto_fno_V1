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

        // Core 4-Pillar Calculation Engines (Singletons)
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
        services.AddSingleton<IPerformanceAnalyticsEngine, PerformanceAnalyticsEngine>();

        // Maturity Levels 4 to 9 Engines
        services.AddSingleton<IHistoricalBacktestEngine, HistoricalBacktestEngine>();
        services.AddSingleton<IShadowPaperTradingEngine, ShadowPaperTradingEngine>();
        services.AddSingleton<IForwardValidationEngine, ForwardValidationEngine>();
        services.AddSingleton<IGuardedLiveExecutionEngine, GuardedLiveExecutionEngine>();
        services.AddSingleton<IAutomatedLiveExecutionEngine, AutomatedLiveExecutionEngine>();
        services.AddSingleton<ISelfOptimizingEngine, SelfOptimizingEngine>();
        services.AddSingleton<ISystemHealthMonitoringEngine, SystemHealthMonitoringEngine>();
        services.AddSingleton<ISystemMaturityDashboardEngine, SystemMaturityDashboardEngine>();

        // Master Coordinator
        services.AddSingleton<IConvictionSystemV2Coordinator, ConvictionSystemV2Coordinator>();

        return services;
    }
}
