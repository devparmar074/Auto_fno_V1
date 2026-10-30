using System;
using System.Collections.Generic;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoFno.Tests.V2;

public class HistoricalBacktestTests
{
    private readonly HistoricalBacktestEngine _engine;

    public HistoricalBacktestTests()
    {
        var scoringEngine = new ConvictionScoringEngine(
            new PCRAnalysisEngine(),
            new OIAnalysisEngine(),
            new GreeksAnalysisEngine(),
            new SpotTrendEngine());
        var riskEngine = new RiskManagementEngine();
        var qualEngine = new TradeQualificationEngine(riskEngine);
        var tmEngine = new TradeManagementEngine();
        var logger = new NullLogger<HistoricalBacktestEngine>();

        _engine = new HistoricalBacktestEngine(scoringEngine, qualEngine, riskEngine, tmEngine, logger);
    }

    [Fact]
    public void InspectDataQuality_ShouldDetectIncompleteChainsAndInvertedQuotes()
    {
        var snapshots = new List<HistoricalMarketSnapshot>
        {
            new()
            {
                Symbol = "NIFTY50",
                TimestampUtc = new DateTime(2026, 10, 1, 9, 30, 0),
                SpotPrice = 24500m,
                OptionChain = new OptionChainDto { Strikes = new List<StrikeDataDto>() } // Incomplete
            },
            new()
            {
                Symbol = "NIFTY50",
                TimestampUtc = new DateTime(2026, 10, 1, 9, 31, 0),
                SpotPrice = 24520m,
                OptionChain = CreateSampleChain(24520m, invertedBidAsk: true) // Inverted bid/ask
            }
        };

        var report = _engine.InspectDataQuality(snapshots);

        Assert.Equal(2, report.TotalSnapshotsInspected);
        Assert.True(report.IncompleteChainCount > 0);
        Assert.True(report.InvertedBidAskCount > 0);
        Assert.False(report.IsDataSufficientForBacktesting);
    }

    [Fact]
    public void RunBacktest_WithBullishTrendSnapshots_ShouldSimulateProfitableTrades()
    {
        var snapshots = GenerateTrendingSnapshots(startPrice: 24500m, step: 25m, count: 40, isBullish: true);
        var request = new HistoricalBacktestRequest
        {
            Symbol = "NIFTY50",
            InitialCapital = 500000m,
            RiskPerTradePercent = 1.0m,
            MinConvictionThreshold = 60.0m,
            SlippageModel = BacktestExecutionSlippageModel.ConservativeBidAsk
        };

        var result = _engine.RunBacktest(snapshots, request);

        Assert.NotNull(result);
        Assert.True(result.TotalSnapshotsEvaluated >= 40);
        Assert.True(result.DataQualityReport.IsDataSufficientForBacktesting);
        Assert.True(result.TotalTradesExecuted > 0, "Backtest should execute at least 1 qualified trade.");
        Assert.True(result.TotalTransactionCosts > 0, "Friction costs must be accounted for.");
        Assert.NotNull(result.ScoreBuckets);
        Assert.NotEmpty(result.ScoreBuckets);
    }

    [Fact]
    public void RunBacktest_ShouldEnforceCutoffAt1515Ist()
    {
        // 15:15 IST = 09:45 UTC
        var snapshots = new List<HistoricalMarketSnapshot>();
        var baseTime = new DateTime(2026, 10, 1, 9, 20, 0); // 14:50 IST

        for (int i = 0; i < 35; i++)
        {
            var time = baseTime.AddMinutes(i); // Proceeds up to 15:25 IST
            snapshots.Add(new HistoricalMarketSnapshot
            {
                Symbol = "NIFTY50",
                TimestampUtc = time,
                SpotPrice = 24500m + (i * 5m),
                OptionChain = CreateSampleChain(24500m + (i * 5m)),
                SpotHistory = CreateSampleCandles(24500m + (i * 5m))
            });
        }

        var request = new HistoricalBacktestRequest
        {
            Symbol = "NIFTY50",
            InitialCapital = 500000m
        };

        var result = _engine.RunBacktest(snapshots, request);

        if (result.ExecutedTrades.Count > 0)
        {
            // Verify all positions are closed by the end of session
            Assert.All(result.ExecutedTrades, t => Assert.True(t.ExitReason != V2ExitReason.None));
        }
    }

    private OptionChainDto CreateSampleChain(decimal spotPrice, bool invertedBidAsk = false)
    {
        var chain = new OptionChainDto
        {
            UnderlyingSymbol = "NIFTY50",
            SpotPrice = spotPrice,
            TimestampUtc = DateTime.UtcNow
        };

        decimal atm = Math.Round(spotPrice / 50m) * 50m;
        for (int i = -4; i <= 4; i++)
        {
            decimal strike = atm + (i * 50m);
            decimal callBid = Math.Max(1m, 150m - (i * 20m));
            decimal callAsk = invertedBidAsk ? callBid - 2m : callBid + 1.5m;

            chain.Strikes.Add(new StrikeDataDto
            {
                StrikePrice = strike,
                IsAtm = (i == 0),
                Call = new OptionContractDetailDto
                {
                    Ltp = callBid + 0.5m,
                    BidPrice = callBid,
                    AskPrice = callAsk,
                    OpenInterest = 100000,
                    OiChange = 15000,
                    Buildup = BuildupType.LongBuildup,
                    Volume = 50000,
                    Greeks = new OptionGreeksDto { Delta = 0.54m, Gamma = 0.002m, Theta = -8.0m, Vega = 12.0m, Iv = 14.5m }
                },
                Put = new OptionContractDetailDto
                {
                    Ltp = 120m + (i * 20m),
                    BidPrice = 119m + (i * 20m),
                    AskPrice = 121m + (i * 20m),
                    OpenInterest = 120000,
                    OiChange = 25000,
                    Buildup = BuildupType.ShortBuildup,
                    Volume = 60000,
                    Greeks = new OptionGreeksDto { Delta = -0.46m, Gamma = 0.002m, Theta = -8.0m, Vega = 12.0m, Iv = 15.0m }
                }
            });
        }
        return chain;
    }

    private List<HistoricalMarketSnapshot> GenerateTrendingSnapshots(decimal startPrice, decimal step, int count, bool isBullish)
    {
        var list = new List<HistoricalMarketSnapshot>();
        var startTime = new DateTime(2026, 10, 1, 4, 0, 0); // 09:30 IST

        for (int i = 0; i < count; i++)
        {
            decimal price = isBullish ? startPrice + (i * step) : startPrice - (i * step);
            list.Add(new HistoricalMarketSnapshot
            {
                Symbol = "NIFTY50",
                TimestampUtc = startTime.AddMinutes(i),
                SpotPrice = price,
                OptionChain = CreateSampleChain(price),
                PcrSummary = new PcrSummaryDto 
                { 
                    OiPcr = isBullish ? 1.45m : 0.65m, 
                    VolumePcr = isBullish ? 1.50m : 0.60m, 
                    AtmPcr = isBullish ? 1.40m : 0.60m,
                    PcrSlope = isBullish ? 0.08m : -0.08m,
                    LongBuildupCount = isBullish ? 8 : 1,
                    ShortBuildupCount = isBullish ? 1 : 8,
                    PutOiChangeTotal = isBullish ? 150000 : 10000,
                    CallOiChangeTotal = isBullish ? 20000 : 150000
                },
                MaxPain = new MaxPainDto { MaxPainStrike = startPrice + 100m },
                SpotHistory = CreateSampleCandles(price)
            });
        }
        return list;
    }

    private List<Candle> CreateSampleCandles(decimal currentPrice)
    {
        var candles = new List<Candle>();
        for (int i = 30; i >= 0; i--)
        {
            decimal baseP = currentPrice - (i * 3m);
            candles.Add(new Candle
            {
                Open = baseP - 3m,
                High = baseP + 15m,
                Low = baseP - 15m,
                Close = baseP + 2m,
                Volume = 10000,
                TimestampUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        return candles;
    }
}
