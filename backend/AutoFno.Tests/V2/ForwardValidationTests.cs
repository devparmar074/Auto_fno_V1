using System;
using System.Collections.Generic;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoFno.Tests.V2;

public class ForwardValidationTests
{
    private readonly ForwardValidationEngine _engine;

    public ForwardValidationTests()
    {
        _engine = new ForwardValidationEngine(new NullLogger<ForwardValidationEngine>());
    }

    [Fact]
    public void StartCampaign_ShouldInitializeWithImmutableBaseline()
    {
        var config = new ForwardValidationCampaignConfig
        {
            BaselineVersion = "v2.0-baseline-release",
            RequiredCalendarDays = 30,
            MinimumTradeSamples = 50
        };

        var status = _engine.StartCampaign(config);

        Assert.Equal("v2.0-baseline-release", status.BaselineVersion);
        Assert.Equal(ValidationCampaignStatus.Active, status.Status);
        Assert.False(status.HasElapsedFullDuration);
        Assert.False(status.HasMetSampleCount);
    }

    [Fact]
    public void RecordDailyObservation_ShouldAccumulateRegimeAndPerformanceMetrics()
    {
        var dayTrades = new List<V2HypotheticalTradeRecord>
        {
            new() { NetPnl = 1500m, GrossPnl = 1540m, OptionType = OptionType.CE, EntryConvictionScore = 75m },
            new() { NetPnl = -400m, GrossPnl = -360m, OptionType = OptionType.CE, EntryConvictionScore = 65m },
            new() { NetPnl = 1200m, GrossPnl = 1240m, OptionType = OptionType.CE, EntryConvictionScore = 85m }
        };

        _engine.RecordDailyObservation(DateTime.UtcNow, dayTrades, MarketRegime.StrongBullTrend);

        var status = _engine.GetCurrentCampaignStatus();

        Assert.Equal(3, status.TotalTradesRecorded);
        Assert.Equal(2, status.WinningTrades);
        Assert.Equal(1, status.LosingTrades);
        Assert.Equal(2300m, status.CurrentNetPnl);
        Assert.Single(status.RegimeBreakdowns);
        Assert.Equal(MarketRegime.StrongBullTrend, status.RegimeBreakdowns[0].Regime);
    }

    [Fact]
    public void EvaluateAcceptanceGates_WhenRequirementsMet_ShouldPassValidation()
    {
        var config = new ForwardValidationCampaignConfig
        {
            BaselineVersion = "v2.0-test",
            StartDateUtc = DateTime.UtcNow.AddDays(-31), // 31 days elapsed
            RequiredCalendarDays = 30,
            MinimumTradeSamples = 10,
            MinimumNetExpectancy = 200m,
            MinimumProfitFactor = 1.5m,
            MinimumWinRatePercent = 55.0m
        };

        _engine.StartCampaign(config);

        // Add 10 winning trades meeting targets
        var trades = new List<V2HypotheticalTradeRecord>();
        for (int i = 0; i < 7; i++)
        {
            trades.Add(new V2HypotheticalTradeRecord { NetPnl = 1000m, GrossPnl = 1040m, EntryConvictionScore = 80m });
        }
        for (int i = 0; i < 3; i++)
        {
            trades.Add(new V2HypotheticalTradeRecord { NetPnl = -300m, GrossPnl = -260m, EntryConvictionScore = 65m });
        }

        _engine.RecordDailyObservation(DateTime.UtcNow, trades, MarketRegime.MildBullBias);

        var status = _engine.EvaluateAcceptanceGates();

        Assert.True(status.HasElapsedFullDuration);
        Assert.True(status.HasMetSampleCount);
        Assert.True(status.CurrentWinRatePercent >= 55.0m);
        Assert.True(status.CurrentProfitFactor >= 1.5m);
        Assert.True(status.HasPassedAllAcceptanceGates);
        Assert.Equal(ValidationCampaignStatus.Completed, status.Status);
    }
}
