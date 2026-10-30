using System;
using System.Collections.Generic;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Xunit;

namespace AutoFno.Tests.V2;

public class TradeQualificationTests
{
    private readonly TradeQualificationEngine _engine = new(new RiskManagementEngine());
    private readonly QuantitativeConvictionV2Config _config = new();

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

    private (V2ConvictionScoreDto score, OptionChainDto chain, List<Candle> candles) CreateValidBullishSetup()
    {
        var candles = GenerateValidCandles(30);

        var chain = new OptionChainDto
        {
            SpotPrice = 24500,
            TimestampUtc = DateTime.UtcNow,
            Strikes = new List<StrikeDataDto>
            {
                new()
                {
                    StrikePrice = 24500,
                    IsAtm = true,
                    Call = new OptionContractDetailDto
                    {
                        TradingSymbol = "NIFTY24500CE",
                        InstrumentKey = "NSE_FO|NIFTY24500CE",
                        Ltp = 120.0m,
                        BidPrice = 119.5m,
                        AskPrice = 120.5m,
                        Volume = 50000,
                        OpenInterest = 200000,
                        Greeks = new OptionGreeksDto { Delta = 0.52m, Theta = -8.0m, Iv = 14.0m }
                    },
                    Put = new OptionContractDetailDto
                    {
                        TradingSymbol = "NIFTY24500PE",
                        InstrumentKey = "NSE_FO|NIFTY24500PE",
                        Ltp = 110.0m,
                        BidPrice = 109.5m,
                        AskPrice = 110.5m,
                        Volume = 40000,
                        OpenInterest = 180000,
                        Greeks = new OptionGreeksDto { Delta = -0.48m, Theta = -8.0m, Iv = 14.0m }
                    }
                }
            }
        };

        var score = new V2ConvictionScoreDto
        {
            IndexSymbol = "NIFTY50",
            SpotPrice = 24500,
            TimestampUtc = DateTime.UtcNow,
            TotalScore = 75.0m,
            Recommendation = StrategyRecommendation.StrongBuyCall,
            CandidateDecision = TradeCandidateDecision.BuyAtmCeCandidate,
            PcrAnalysis = new V2PcrAnalysisResult { TotalPcrPillarScore = 20m },
            OiAnalysis = new V2OiAnalysisResult { TotalOiPillarScore = 18m, CallWallStrike = 25000m, PutWallStrike = 24000m },
            GreeksAnalysis = new V2GreeksAnalysisResult
            {
                TotalGreeksPillarScore = 18m,
                IvEnvironment = IvEnvironment.StableNormal,
                EstimatedPremiumChangeCe = 12.0m,
                EstimatedPremiumChangePe = 5.0m
            },
            SpotTrend = new V2SpotTrendResult
            {
                TotalSpotPillarScore = 19m,
                IsSupertrendBullish = true,
                Ema21 = 24450m,
                Vwap = 24450m,
                IsOverextended = false
            }
        };

        return (score, chain, candles);
    }

    [Fact]
    public void Valid_Bullish_Setup_Qualifies_Successfully()
    {
        var (score, chain, candles) = CreateValidBullishSetup();
        var candidate = _engine.QualifyTrade(score, chain, candles, _config, currentOpenPositionsCount: 0, todayRealizedPnl: 0m, consecutiveLosses: 0);

        Assert.True(candidate.IsQualified);
        Assert.Equal(TradeCandidateDecision.BuyAtmCeCandidate, candidate.Decision);
        Assert.Equal(QualificationRejectionReason.None, candidate.RejectionReasons);
        Assert.True(candidate.AllocatedLots > 0);
        Assert.True(candidate.RiskRewardRatio >= 1.5m);
    }

    [Fact]
    public void Rejects_When_Conviction_Score_Below_Threshold()
    {
        var (score, chain, candles) = CreateValidBullishSetup();
        score.TotalScore = 55.0m; // Below 60 threshold

        var candidate = _engine.QualifyTrade(score, chain, candles, _config, 0, 0m, 0);

        Assert.False(candidate.IsQualified);
        Assert.True(candidate.RejectionReasons.HasFlag(QualificationRejectionReason.ScoreBelowThreshold));
    }

    [Fact]
    public void Rejects_When_Spot_Trend_Mismatches_Direction()
    {
        var (score, chain, candles) = CreateValidBullishSetup();
        score.SpotTrend.IsSupertrendBullish = false;
        score.SpotPrice = 24400m;
        score.SpotTrend.Ema21 = 24450m; // Spot below EMA21 & Supertrend bearish

        var candidate = _engine.QualifyTrade(score, chain, candles, _config, 0, 0m, 0);

        Assert.False(candidate.IsQualified);
        Assert.True(candidate.RejectionReasons.HasFlag(QualificationRejectionReason.SpotTrendMismatch));
    }

    [Fact]
    public void Rejects_When_IV_Regime_Presents_Crush_Risk()
    {
        var (score, chain, candles) = CreateValidBullishSetup();
        score.GreeksAnalysis.IvEnvironment = IvEnvironment.CrushRisk;

        var candidate = _engine.QualifyTrade(score, chain, candles, _config, 0, 0m, 0);

        Assert.False(candidate.IsQualified);
        Assert.True(candidate.RejectionReasons.HasFlag(QualificationRejectionReason.UnfavorableIVRegime));
    }

    [Fact]
    public void Rejects_When_Daily_Loss_Limit_Reached()
    {
        var (score, chain, candles) = CreateValidBullishSetup();
        decimal maxDailyLoss = _config.MaxDailyLossLimit; // Rs 5000

        var candidate = _engine.QualifyTrade(score, chain, candles, _config, 0, todayRealizedPnl: -(maxDailyLoss + 100m), consecutiveLosses: 0);

        Assert.False(candidate.IsQualified);
        Assert.True(candidate.RejectionReasons.HasFlag(QualificationRejectionReason.DailyLossLimitReached));
    }

    [Fact]
    public void Rejects_When_Max_Positions_Reached()
    {
        var (score, chain, candles) = CreateValidBullishSetup();

        var candidate = _engine.QualifyTrade(score, chain, candles, _config, currentOpenPositionsCount: _config.MaxSimultaneousPositions, todayRealizedPnl: 0m, consecutiveLosses: 0);

        Assert.False(candidate.IsQualified);
        Assert.True(candidate.RejectionReasons.HasFlag(QualificationRejectionReason.DailyLossLimitReached));
    }

    [Fact]
    public void Rejects_When_Option_Liquidity_Is_Low()
    {
        var (score, chain, candles) = CreateValidBullishSetup();
        chain.Strikes[0].Call!.OpenInterest = 1000; // Below 50,000 threshold

        var candidate = _engine.QualifyTrade(score, chain, candles, _config, 0, 0m, 0);

        Assert.False(candidate.IsQualified);
        Assert.True(candidate.RejectionReasons.HasFlag(QualificationRejectionReason.OptionLiquidityInsufficient));
    }
}
