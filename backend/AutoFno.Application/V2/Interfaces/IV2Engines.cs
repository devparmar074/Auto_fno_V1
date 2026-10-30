using System;
using System.Collections.Generic;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Interfaces;

public interface IMarketDataValidationEngine
{
    bool ValidateMarketData(OptionChainDto? optionChain, List<Candle>? spotCandles, out List<string> validationErrors);
}

public interface IPCRAnalysisEngine
{
    V2PcrAnalysisResult Analyze(OptionChainDto optionChain, PcrSummaryDto pcrSummary, QuantitativeConvictionV2Config config);
}

public interface IOIAnalysisEngine
{
    V2OiAnalysisResult Analyze(OptionChainDto optionChain, PcrSummaryDto pcrSummary, MaxPainDto maxPain, QuantitativeConvictionV2Config config);
}

public interface IGreeksAnalysisEngine
{
    V2GreeksAnalysisResult Analyze(OptionChainDto optionChain, decimal spotPrice, QuantitativeConvictionV2Config config);
    decimal EstimateOptionPremiumMovement(decimal dS, decimal delta, decimal gamma, decimal theta, decimal vega, decimal dIv, decimal dtDays);
}

public interface ISpotTrendEngine
{
    V2SpotTrendResult Analyze(List<Candle> spotCandles, decimal spotPrice, QuantitativeConvictionV2Config config);
}

public interface IConvictionScoringEngineV2
{
    V2ConvictionScoreDto CalculateConvictionScore(
        OptionChainDto optionChain,
        PcrSummaryDto pcrSummary,
        MaxPainDto maxPain,
        List<Candle> spotCandles,
        QuantitativeConvictionV2Config config);

    V2ConvictionScoreDto CalculateScoreFromPillars(
        string symbol,
        decimal spotPrice,
        V2PcrAnalysisResult pcrResult,
        V2OiAnalysisResult oiResult,
        V2GreeksAnalysisResult greeksResult,
        V2SpotTrendResult spotResult,
        QuantitativeConvictionV2Config config);
}

public interface ITradeQualificationEngine
{
    V2TradeCandidate QualifyTrade(
        V2ConvictionScoreDto score,
        OptionChainDto optionChain,
        List<Candle> spotCandles,
        QuantitativeConvictionV2Config config,
        int currentOpenPositionsCount,
        decimal todayRealizedPnl,
        int consecutiveLosses = 0);
}

public interface IRiskManagementEngineV2
{
    void CalculatePositionSizing(V2TradeCandidate candidate, QuantitativeConvictionV2Config config, decimal todayRealizedLoss);
    bool IsRiskPermitted(
        V2TradeCandidate candidate,
        QuantitativeConvictionV2Config config,
        int currentOpenPositionsCount,
        decimal todayRealizedPnl,
        int consecutiveLosses,
        out string rejectionReason);
}

public interface ITradeManagementEngineV2
{
    void UpdatePosition(
        V2HypotheticalPosition position,
        decimal currentSpot,
        decimal currentOptionPrice,
        DateTime currentTimeUtc,
        QuantitativeConvictionV2Config config,
        out V2ExitReason exitReason);

    V2HypotheticalTradeRecord ClosePosition(
        V2HypotheticalPosition position,
        decimal exitPrice,
        V2ExitReason reason,
        DateTime exitTimeUtc,
        QuantitativeConvictionV2Config config);
}

public interface IPerformanceAnalyticsEngine
{
    void RecordSignal(V2ConvictionScoreDto score, V2TradeCandidate candidate);
    void RecordTrade(V2HypotheticalTradeRecord trade);
    V2PerformanceMetricsDto GetPerformanceMetrics();
    void Reset();
}

public interface IStrategyComparisonEngine
{
    V1VsV2ComparisonDto Compare(
        FnOStrategyScoreDto? v1Score,
        V2ConvictionScoreDto? v2Score,
        V2PerformanceMetricsDto? v2Metrics);
}
