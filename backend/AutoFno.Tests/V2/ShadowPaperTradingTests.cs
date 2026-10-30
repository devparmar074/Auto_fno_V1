using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoFno.Tests.V2;

public class ShadowPaperTradingTests : IDisposable
{
    private readonly ShadowPaperTradingEngine _shadowEngine;
    private readonly QuantitativeConvictionV2Config _config;
    private readonly string _storageDir;

    public ShadowPaperTradingTests()
    {
        _storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "v2_shadow");
        CleanupStorage();

        _config = new QuantitativeConvictionV2Config
        {
            EnableV2 = true,
            V2ShadowMode = true,
            AccountCapital = 500000m,
            RiskPerTradePercent = 0.5m,
            MaxSimultaneousPositions = 5
        };

        var tmEngine = new TradeManagementEngine();
        var analyticsEngine = new PerformanceAnalyticsEngine();
        var logger = new NullLogger<ShadowPaperTradingEngine>();

        _shadowEngine = new ShadowPaperTradingEngine(_config, tmEngine, analyticsEngine, logger);
    }

    public void Dispose()
    {
        CleanupStorage();
    }

    private void CleanupStorage()
    {
        try
        {
            if (Directory.Exists(_storageDir))
            {
                Directory.Delete(_storageDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task ProcessLiveSnapshot_WithQualifiedCandidate_ShouldOpenShadowPosition()
    {
        await _shadowEngine.InitializeAsync();

        var chain = CreateSampleChain(24500m);
        var score = new V2ConvictionScoreDto { TotalScore = 75.0m };
        var candidate = new V2TradeCandidate
        {
            IsQualified = true,
            Decision = TradeCandidateDecision.BuyAtmCeCandidate,
            IndexSymbol = "NIFTY50",
            TradingSymbol = "NIFTY 24500 CE",
            TargetStrike = 24500m,
            OptionType = OptionType.CE,
            ExpectedEntryPrice = 145m,
            StopLossPrice = 115m,
            Target1Price = 175m,
            Target2Price = 205m,
            AllocatedLots = 1,
            AllocatedQuantity = 75,
            EstimatedCostPerLot = 40m
        };

        await _shadowEngine.ProcessLiveSnapshotAsync(chain, score, candidate);

        var openPositions = await _shadowEngine.GetActivePositionsAsync();
        Assert.Single(openPositions);
        Assert.Equal("NIFTY 24500 CE", openPositions[0].TradingSymbol);
        Assert.Equal(145m, openPositions[0].EntryPrice);
    }

    [Fact]
    public async Task ProcessLiveSnapshot_WithDuplicateQuote_ShouldIgnoreDuplicateEvent()
    {
        await _shadowEngine.InitializeAsync();

        var fixedTime = new DateTime(2026, 10, 1, 10, 0, 0);
        var chain = CreateSampleChain(24500m);
        chain.TimestampUtc = fixedTime;

        var score = new V2ConvictionScoreDto { TotalScore = 72m };
        var candidate = new V2TradeCandidate
        {
            IsQualified = true,
            Decision = TradeCandidateDecision.BuyAtmCeCandidate,
            TradingSymbol = "NIFTY 24500 CE",
            TargetStrike = 24500m,
            OptionType = OptionType.CE,
            AllocatedLots = 1,
            AllocatedQuantity = 75
        };

        // First event
        await _shadowEngine.ProcessLiveSnapshotAsync(chain, score, candidate);
        var count1 = (await _shadowEngine.GetActivePositionsAsync()).Count;

        // Duplicate event with identical quote and timestamp
        await _shadowEngine.ProcessLiveSnapshotAsync(chain, score, candidate);
        var count2 = (await _shadowEngine.GetActivePositionsAsync()).Count;

        Assert.Equal(count1, count2);
    }

    [Fact]
    public async Task PersistentRestartRecovery_ShouldRestoreOpenPositionsFromDisk()
    {
        // 1. Initialize and open a position
        await _shadowEngine.InitializeAsync();
        var chain = CreateSampleChain(24500m);
        var candidate = new V2TradeCandidate
        {
            IsQualified = true,
            Decision = TradeCandidateDecision.BuyAtmCeCandidate,
            TradingSymbol = "NIFTY 24500 CE RESTART TEST",
            TargetStrike = 24500m,
            OptionType = OptionType.CE,
            AllocatedLots = 1,
            AllocatedQuantity = 75,
            ExpectedEntryPrice = 120m,
            StopLossPrice = 90m,
            Target1Price = 150m
        };

        await _shadowEngine.ProcessLiveSnapshotAsync(chain, new V2ConvictionScoreDto(), candidate);

        // 2. Create brand new engine instance pointing to same storage
        var newEngine = new ShadowPaperTradingEngine(
            _config,
            new TradeManagementEngine(),
            new PerformanceAnalyticsEngine(),
            new NullLogger<ShadowPaperTradingEngine>());

        await newEngine.InitializeAsync();

        var restoredPositions = await newEngine.GetActivePositionsAsync();
        Assert.NotEmpty(restoredPositions);
        Assert.Contains(restoredPositions, p => p.TradingSymbol == "NIFTY 24500 CE RESTART TEST");
    }

    private OptionChainDto CreateSampleChain(decimal spotPrice)
    {
        var chain = new OptionChainDto
        {
            UnderlyingSymbol = "NIFTY50",
            SpotPrice = spotPrice,
            TimestampUtc = DateTime.UtcNow
        };

        decimal atm = Math.Round(spotPrice / 50m) * 50m;
        chain.Strikes.Add(new StrikeDataDto
        {
            StrikePrice = atm,
            Call = new OptionContractDetailDto
            {
                Ltp = 145m,
                BidPrice = 144.5m,
                AskPrice = 145.5m,
                OpenInterest = 100000,
                Volume = 50000
            },
            Put = new OptionContractDetailDto
            {
                Ltp = 120m,
                BidPrice = 119.5m,
                AskPrice = 120.5m,
                OpenInterest = 100000,
                Volume = 50000
            }
        });
        return chain;
    }
}
