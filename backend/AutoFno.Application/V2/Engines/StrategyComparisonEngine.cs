using System;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class StrategyComparisonEngine : IStrategyComparisonEngine
{
    public V1VsV2ComparisonDto Compare(
        FnOStrategyScoreDto? v1Score,
        V2ConvictionScoreDto? v2Score,
        V2PerformanceMetricsDto? v2Metrics)
    {
        var metrics = v2Metrics ?? new V2PerformanceMetricsDto();

        var comp = new V1VsV2ComparisonDto
        {
            V1TotalSignals = v1Score != null ? 1 : 0,
            V2TotalSignals = metrics.TotalSignalsEvaluated,
            V1QualifiedTrades = v1Score != null && (v1Score.TotalScore >= 60 || v1Score.TotalScore <= -60) ? 1 : 0,
            V2QualifiedTrades = metrics.TotalTradesExecuted,
            V1WinRate = 50.0m, // Baseline historical benchmark
            V2HypotheticalWinRate = metrics.WinRatePercent,
            V1NetPnl = 0m,
            V2HypotheticalNetPnl = metrics.NetPnl,
            V1ProfitFactor = 1.2m,
            V2ProfitFactor = metrics.ProfitFactor,
            V1MaxDrawdown = 5000.0m,
            V2MaxDrawdown = metrics.MaxDrawdownRupees,
            V1AvgRiskPerTrade = 2500.0m,
            V2AvgRiskPerTrade = metrics.AverageLoss > 0 ? metrics.AverageLoss : 1250.0m,
            V2NetExpectancy = metrics.NetExpectancyRupees
        };

        if (metrics.TotalTradesExecuted == 0)
        {
            comp.SummaryEvaluation = "V2 Shadow Mode active. Accumulating hypothetical trades to evaluate statistical edge over V1 baseline.";
        }
        else if (metrics.NetExpectancyRupees > 0 && metrics.ProfitFactor > 1.5m)
        {
            comp.SummaryEvaluation = $"V2 Demonstrating Superior Edge: Net Expectancy +Rs {metrics.NetExpectancyRupees:N2}/trade, Profit Factor {metrics.ProfitFactor:F2}, Win Rate {metrics.WinRatePercent:F1}%.";
        }
        else if (metrics.NetExpectancyRupees <= 0)
        {
            comp.SummaryEvaluation = $"V2 Negative/Flat Expectancy (Rs {metrics.NetExpectancyRupees:N2}). Preserving V1 execution; V2 requires parameter recalibration.";
        }
        else
        {
            comp.SummaryEvaluation = $"V2 Moderate Edge (PF: {metrics.ProfitFactor:F2}). Shadow monitoring continuing.";
        }

        return comp;
    }
}
