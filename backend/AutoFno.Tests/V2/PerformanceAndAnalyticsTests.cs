using System;
using System.Collections.Generic;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Xunit;

namespace AutoFno.Tests.V2;

public class PerformanceAndAnalyticsTests
{
    private readonly PerformanceAnalyticsEngine _analytics = new();
    private readonly StrategyComparisonEngine _comparison = new();

    [Fact]
    public void Performance_Metrics_Calculates_Net_Expectancy_And_Buckets()
    {
        _analytics.Reset();

        // Trade 1: Win (+Rs 1500 net), Score 72
        _analytics.RecordTrade(new V2HypotheticalTradeRecord
        {
            TradeId = Guid.NewGuid(),
            IndexSymbol = "NIFTY50",
            OptionType = OptionType.CE,
            Lots = 2,
            Quantity = 50,
            EntryConvictionScore = 72m,
            EntryPrice = 100m,
            ExitPrice = 135m,
            GrossPnl = 1750m,
            EstimatedCosts = 250m,
            NetPnl = 1500m,
            ExitReason = V2ExitReason.Target1Hit,
            EntryTimeUtc = DateTime.UtcNow.AddHours(-2),
            ExitTimeUtc = DateTime.UtcNow.AddHours(-1)
        });

        // Trade 2: Win (+Rs 2000 net), Score 85
        _analytics.RecordTrade(new V2HypotheticalTradeRecord
        {
            TradeId = Guid.NewGuid(),
            IndexSymbol = "NIFTY50",
            OptionType = OptionType.CE,
            Lots = 2,
            Quantity = 50,
            EntryConvictionScore = 85m,
            EntryPrice = 100m,
            ExitPrice = 145m,
            GrossPnl = 2250m,
            EstimatedCosts = 250m,
            NetPnl = 2000m,
            ExitReason = V2ExitReason.Target2Hit,
            EntryTimeUtc = DateTime.UtcNow.AddHours(-2),
            ExitTimeUtc = DateTime.UtcNow.AddHours(-1)
        });

        // Trade 3: Loss (-Rs 1000 net), Score 65
        _analytics.RecordTrade(new V2HypotheticalTradeRecord
        {
            TradeId = Guid.NewGuid(),
            IndexSymbol = "NIFTY50",
            OptionType = OptionType.PE,
            Lots = 2,
            Quantity = 50,
            EntryConvictionScore = 65m,
            EntryPrice = 100m,
            ExitPrice = 85m,
            GrossPnl = -750m,
            EstimatedCosts = 250m,
            NetPnl = -1000m,
            ExitReason = V2ExitReason.UnderlyingInvalidation,
            EntryTimeUtc = DateTime.UtcNow.AddHours(-2),
            ExitTimeUtc = DateTime.UtcNow.AddHours(-1)
        });

        var metrics = _analytics.GetPerformanceMetrics();

        Assert.Equal(3, metrics.TotalTradesExecuted);
        Assert.Equal(2, metrics.WinningTradesCount);
        Assert.Equal(1, metrics.LosingTradesCount);
        Assert.Equal(66.67m, metrics.WinRatePercent);
        Assert.Equal(2500m, metrics.NetPnl);
        Assert.Equal(1750m, metrics.AverageWin); // (1500 + 2000) / 2
        Assert.Equal(1000m, metrics.AverageLoss);
        Assert.True(metrics.ProfitFactor > 1.0m);
        Assert.True(metrics.NetExpectancyRupees > 0m, "Expectancy per trade must be positive");

        // Verify score buckets:
        // +70 to +79 bucket: 1 trade (win)
        var bucket70_79 = metrics.ScoreBuckets.Find(b => b.BucketRange == "+70 to +79");
        Assert.NotNull(bucket70_79);
        Assert.Equal(1, bucket70_79.TradesCount);
        Assert.Equal(100.0m, bucket70_79.WinRatePercent);

        // +80 to +89 bucket: 1 trade (win)
        var bucket80_89 = metrics.ScoreBuckets.Find(b => b.BucketRange == "+80 to +89");
        Assert.NotNull(bucket80_89);
        Assert.Equal(1, bucket80_89.TradesCount);

        // +60 to +69 bucket: 1 trade (loss)
        var bucket60_69 = metrics.ScoreBuckets.Find(b => b.BucketRange == "+60 to +69");
        Assert.NotNull(bucket60_69);
        Assert.Equal(1, bucket60_69.TradesCount);
        Assert.Equal(0.0m, bucket60_69.WinRatePercent);
    }

    [Fact]
    public void Strategy_Comparison_Correctly_Contrasts_V1_And_V2()
    {
        var v1Score = new FnOStrategyScoreDto
        {
            IndexSymbol = "NIFTY50",
            TotalScore = 45,
            Recommendation = StrategyRecommendation.Neutral,
            SpotPrice = 24500
        };

        var v2Score = new V2ConvictionScoreDto
        {
            IndexSymbol = "NIFTY50",
            TotalScore = 78m,
            Recommendation = StrategyRecommendation.StrongBuyCall,
            CandidateDecision = TradeCandidateDecision.BuyAtmCeCandidate,
            SpotPrice = 24500
        };

        var v2Metrics = new V2PerformanceMetricsDto
        {
            TotalTradesExecuted = 20,
            WinRatePercent = 65.0m,
            ProfitFactor = 2.1m,
            NetPnl = 35000m
        };

        var comparison = _comparison.Compare(v1Score, v2Score, v2Metrics);

        Assert.Equal(65.0m, comparison.V2HypotheticalWinRate);
        Assert.Equal(2.1m, comparison.V2ProfitFactor);
        Assert.Equal(35000m, comparison.V2HypotheticalNetPnl);
        Assert.False(string.IsNullOrEmpty(comparison.SummaryEvaluation));
    }
}
