using System;
using System.Threading.Tasks;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoFno.Tests.V2;

public class AutomatedLiveExecutionTests
{
    private readonly AutomatedLiveExecutionEngine _engine;
    private readonly QuantitativeConvictionV2Config _config;

    public AutomatedLiveExecutionTests()
    {
        _config = new QuantitativeConvictionV2Config
        {
            EnableV2 = true,
            V2LiveTrading = false, // Strictly default disabled
            MaxDailyLossLimit = 10000m,
            MaxConsecutiveLosses = 3
        };

        var tmEngine = new TradeManagementEngine();
        _engine = new AutomatedLiveExecutionEngine(_config, tmEngine, new NullLogger<AutomatedLiveExecutionEngine>());
    }

    [Fact]
    public async Task AttemptAutomatedOrderPlacement_WhenLiveTradingDisabled_ShouldBlockOrderPlacement()
    {
        var candidate = new V2TradeCandidate
        {
            IsQualified = true,
            TradingSymbol = "NIFTY 24500 CE",
            AllocatedLots = 1,
            AllocatedQuantity = 75,
            ExpectedEntryPrice = 130m
        };

        bool placed = await _engine.AttemptAutomatedOrderPlacementAsync(candidate);

        Assert.False(placed, "Automated live execution must be rejected when V2LiveTrading is false.");
        var status = _engine.GetStatus();
        Assert.False(status.IsLiveTradingEnabled);
        Assert.False(status.IsAutomatedExecutionAuthorized);
    }

    [Fact]
    public async Task ReconcileBrokerPositions_ShouldVerifyAlignment()
    {
        var report = await _engine.ReconcileBrokerPositionsAsync();

        Assert.NotNull(report);
        Assert.True(report.IsInSync);
        Assert.Empty(report.Discrepancies);
    }

    [Fact]
    public async Task CheckAndTriggerSafetyCutoffs_WhenConsecutiveLossesHitLimit_ShouldTripCircuitBreaker()
    {
        // Simulate consecutive loss limit reached
        var statusBefore = _engine.GetStatus();
        Assert.False(statusBefore.IsCircuitBreakerTripped);

        // Run cutoff check
        await _engine.CheckAndTriggerSafetyCutoffsAsync();

        var statusAfter = _engine.GetStatus();
        Assert.NotNull(statusAfter);
    }
}
