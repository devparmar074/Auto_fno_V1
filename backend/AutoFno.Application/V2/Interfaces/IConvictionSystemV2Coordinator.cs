using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Interfaces;

public interface IConvictionSystemV2Coordinator
{
    QuantitativeConvictionV2Config Config { get; }
    void UpdateConfig(QuantitativeConvictionV2Config newConfig);

    Task<V2ConvictionScoreDto> EvaluateV2SetupAsync(
        OptionChainDto optionChain,
        PcrSummaryDto pcrSummary,
        MaxPainDto maxPain,
        List<Candle> spotCandles,
        CancellationToken ct = default);

    V2TradeCandidate GetLatestCandidate();
    V2ConvictionScoreDto GetLatestScore();
    V2PerformanceMetricsDto GetShadowPerformance();
    List<V2HypotheticalPosition> GetActiveShadowPositions();
    V1VsV2ComparisonDto GetComparisonReport(FnOStrategyScoreDto? v1Score);

    // Sub-Engines for Maturity Levels 4 to 9
    IHistoricalBacktestEngine BacktestEngine { get; }
    IShadowPaperTradingEngine ShadowEngine { get; }
    IForwardValidationEngine ForwardValidationEngine { get; }
    IGuardedLiveExecutionEngine GuardedLiveEngine { get; }
    IAutomatedLiveExecutionEngine AutomatedLiveEngine { get; }
    ISelfOptimizingEngine SelfOptimizingEngine { get; }
    ISystemHealthMonitoringEngine HealthMonitoringEngine { get; }
    ISystemMaturityDashboardEngine MaturityDashboardEngine { get; }
}
