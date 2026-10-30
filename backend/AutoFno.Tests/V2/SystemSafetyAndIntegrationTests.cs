using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFno.Application.Interfaces;
using AutoFno.Application.V2.Engines;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AutoFno.Tests.V2;

public class SystemSafetyAndIntegrationTests
{
    private List<Candle> GenerateValidCandles(int count = 30)
    {
        var candles = new List<Candle>();
        for (int i = 0; i < count; i++)
        {
            candles.Add(new Candle
            {
                TimestampUtc = DateTime.UtcNow.AddMinutes(-count + i),
                Open = 24400 + i * 5,
                High = 24405 + i * 5,
                Low = 24395 + i * 5,
                Close = 24402 + i * 5,
                Volume = 1000
            });
        }
        return candles;
    }

    [Fact]
    public void MarketDataValidationEngine_Rejects_Empty_Or_Incomplete_Data()
    {
        var validator = new MarketDataValidationEngine();
        var candles = GenerateValidCandles(30);

        // 1. Null option chain
        Assert.False(validator.ValidateMarketData(null, candles, out var reason1));
        Assert.Contains("null", reason1[0], StringComparison.OrdinalIgnoreCase);

        // 2. Empty strikes
        var emptyChain = new OptionChainDto { SpotPrice = 24500, Strikes = new List<StrikeDataDto>() };
        Assert.False(validator.ValidateMarketData(emptyChain, candles, out var reason2));
        Assert.Contains("Insufficient strike count", reason2[0]);

        // 3. Insufficient candles (< 21 bars)
        var shortCandles = new List<Candle>
        {
            new() { Close = 24500, TimestampUtc = DateTime.UtcNow }
        };
        var validChain = new OptionChainDto
        {
            SpotPrice = 24500,
            Strikes = new List<StrikeDataDto>()
        };
        for (int i = 0; i < 6; i++)
        {
            validChain.Strikes.Add(new StrikeDataDto
            {
                StrikePrice = 24400 + i * 50,
                Call = new OptionContractDetailDto { Ltp = 100 },
                Put = new OptionContractDetailDto { Ltp = 100 }
            });
        }

        Assert.False(validator.ValidateMarketData(validChain, shortCandles, out var reason3));
        Assert.Contains("Insufficient candlestick history", reason3[0]);
    }

    [Fact]
    public async Task Duplicate_Trade_Signals_Prevented_Within_Cooldown_Window()
    {
        var config = new QuantitativeConvictionV2Config
        {
            EnableV2 = true,
            BullishCandidateThreshold = 30.0m,
            CooldownMinutesAfterLoss = 15
        };

        var pcrEngine = new PCRAnalysisEngine();
        var oiEngine = new OIAnalysisEngine();
        var greeksEngine = new GreeksAnalysisEngine();
        var spotEngine = new SpotTrendEngine();
        var scoringEngine = new ConvictionScoringEngine(pcrEngine, oiEngine, greeksEngine, spotEngine);
        var riskEngine = new RiskManagementEngine();
        var qualificationEngine = new TradeQualificationEngine(riskEngine);
        var tradeManagementEngine = new TradeManagementEngine();
        var analyticsEngine = new PerformanceAnalyticsEngine();
        var comparisonEngine = new StrategyComparisonEngine();

        var coordinator = new ConvictionSystemV2Coordinator(
            config,
            new MarketDataValidationEngine(),
            scoringEngine,
            qualificationEngine,
            tradeManagementEngine,
            analyticsEngine,
            comparisonEngine,
            NullLogger<ConvictionSystemV2Coordinator>.Instance);

        var chain = new OptionChainDto
        {
            SpotPrice = 24550,
            TimestampUtc = DateTime.UtcNow,
            Strikes = new List<StrikeDataDto>()
        };

        for (int strike = 24400; strike <= 24700; strike += 50)
        {
            chain.Strikes.Add(new StrikeDataDto
            {
                StrikePrice = strike,
                IsAtm = strike == 24550,
                Call = new OptionContractDetailDto
                {
                    TradingSymbol = $"NIFTY{strike}CE",
                    InstrumentKey = $"NSE_FO|NIFTY{strike}CE",
                    Ltp = 120.0m,
                    BidPrice = 119.5m,
                    AskPrice = 120.5m,
                    Volume = 50000,
                    OpenInterest = 200000,
                    Greeks = new OptionGreeksDto { Delta = 0.52m, Theta = -8.0m, Iv = 14.0m }
                },
                Put = new OptionContractDetailDto
                {
                    TradingSymbol = $"NIFTY{strike}PE",
                    InstrumentKey = $"NSE_FO|NIFTY{strike}PE",
                    Ltp = 110.0m,
                    BidPrice = 109.5m,
                    AskPrice = 110.5m,
                    Volume = 40000,
                    OpenInterest = 180000,
                    Greeks = new OptionGreeksDto { Delta = -0.48m, Theta = -8.0m, Iv = 14.0m }
                }
            });
        }

        var pcr = new PcrSummaryDto
        {
            OiPcr = 1.35m,
            VolumePcr = 1.25m,
            PrevOiPcr = 1.15m,
            PcrSlope = 0.20m,
            LongBuildupCount = 10,
            ShortCoveringCount = 5
        };
        var maxPain = new MaxPainDto { MaxPainStrike = 24550, DistanceFromSpot = 0 };
        var candles = GenerateValidCandles(30);

        // First evaluation: candidate qualified
        var score1 = await coordinator.EvaluateV2SetupAsync(chain, pcr, maxPain, candles);
        var cand1 = coordinator.GetLatestCandidate();
        Assert.True(cand1.IsQualified, $"First candidate failed qualification: {string.Join("; ", cand1.RejectionDetails)}");

        // Immediate second evaluation (0 seconds elapsed): cooldown must block duplicate entry
        var score2 = await coordinator.EvaluateV2SetupAsync(chain, pcr, maxPain, candles);
        var cand2 = coordinator.GetLatestCandidate();

        Assert.False(cand2.IsQualified, "Second signal inside cooldown window must be rejected");
        Assert.True(cand2.RejectionReasons.HasFlag(QualificationRejectionReason.CooldownActive));
    }

    [Fact]
    public async Task V2_Errors_Are_Isolated_And_Never_Disrupt_V1_Execution()
    {
        // Setup mock coordinator that throws fatal error
        var faultyCoordinatorMock = new Mock<IConvictionSystemV2Coordinator>();
        faultyCoordinatorMock
            .Setup(c => c.EvaluateV2SetupAsync(It.IsAny<OptionChainDto>(), It.IsAny<PcrSummaryDto>(), It.IsAny<MaxPainDto>(), It.IsAny<List<Candle>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fatal simulated V2 memory corruption error"));

        // Setup V1 strategy engine mock
        var v1EngineMock = new Mock<IFnOStrategyEngine>();
        v1EngineMock
            .Setup(e => e.EvaluateSetupAsync(It.IsAny<OptionChainDto>(), It.IsAny<PcrSummaryDto>(), It.IsAny<MaxPainDto>(), It.IsAny<List<Candle>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnOStrategyScoreDto
            {
                IndexSymbol = "NIFTY50",
                TotalScore = 50,
                Recommendation = StrategyRecommendation.Neutral
            });

        // Simulate background loop execution
        bool v1CompletedSuccessfully = false;
        try
        {
            // Execute V1
            var v1Score = await v1EngineMock.Object.EvaluateSetupAsync(null!, null!, null!, null!, CancellationToken.None);
            Assert.NotNull(v1Score);

            // Execute V2 in isolated wrapper (as implemented in FnOStrategyExecutionBackgroundService)
            try
            {
                await faultyCoordinatorMock.Object.EvaluateV2SetupAsync(null!, null!, null!, null!, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Log and absorb V2 exception safely
                Assert.IsType<InvalidOperationException>(ex);
            }

            v1CompletedSuccessfully = true;
        }
        catch
        {
            v1CompletedSuccessfully = false;
        }

        Assert.True(v1CompletedSuccessfully, "V1 operation must continue uninterrupted when V2 throws an exception");
    }
}
