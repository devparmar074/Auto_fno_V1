using AutoFno.Application.Interfaces;
using AutoFno.Application.Services.Mathematics;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.Services;

public class FnOStrategyEngine : IFnOStrategyEngine
{
    private readonly ILogger<FnOStrategyEngine> _logger;

    public FnOStrategyEngine(ILogger<FnOStrategyEngine> logger)
    {
        _logger = logger;
    }

    public Task<FnOStrategyScoreDto> EvaluateSetupAsync(
        OptionChainDto optionChain,
        PcrSummaryDto pcrSummary,
        MaxPainDto maxPain,
        List<Candle> spotCandles,
        CancellationToken ct = default)
    {
        var spot = optionChain.SpotPrice;
        if (spot <= 0 && spotCandles.Count > 0)
        {
            spot = spotCandles.Last().Close;
        }

        // 1. Spot Technical Indicators
        var tech = SpotIndicatorCalculator.Calculate(spotCandles);

        // 2. Pillar 1: PCR Sentiment Score (Max 25 pts)
        decimal pcrScore = 0;
        var oiPcr = pcrSummary.OiPcr;
        if (oiPcr >= 1.30m)
        {
            pcrScore = 20m;
            if (oiPcr >= 1.45m && oiPcr <= 1.65m) pcrScore = 25m;
            else if (oiPcr > 1.65m) pcrScore = 15m; // Exhaustion fade penalty
        }
        else if (oiPcr >= 1.05m)
        {
            pcrScore = 12m;
        }
        else if (oiPcr >= 0.90m)
        {
            pcrScore = 0m; // Neutral
        }
        else if (oiPcr >= 0.70m)
        {
            pcrScore = -12m;
        }
        else
        {
            pcrScore = -20m;
            if (oiPcr <= 0.55m && oiPcr >= 0.40m) pcrScore = -25m;
            else if (oiPcr < 0.40m) pcrScore = -15m; // Short squeeze bounce risk
        }

        // Volume PCR bonus
        if (pcrSummary.VolumePcr >= 1.15m && pcrScore > 0) pcrScore = Math.Min(25m, pcrScore + 3m);
        else if (pcrSummary.VolumePcr <= 0.85m && pcrScore < 0) pcrScore = Math.Max(-25m, pcrScore - 3m);

        // 3. Pillar 2: Open Interest & Max Pain Gravity Score (Max 25 pts)
        decimal oiScore = 0;
        var totalBullishBuildup = pcrSummary.LongBuildupCount + pcrSummary.ShortCoveringCount;
        var totalBearishBuildup = pcrSummary.ShortBuildupCount + pcrSummary.LongUnwindingCount;
        var netBuildup = totalBullishBuildup - totalBearishBuildup;

        if (netBuildup > 3) oiScore += 12m;
        else if (netBuildup < -3) oiScore -= 12m;

        // Fresh OI addition (Put Writing vs Call Writing)
        if (pcrSummary.PutOiChangeTotal > pcrSummary.CallOiChangeTotal * 1.2m) oiScore += 8m;
        else if (pcrSummary.CallOiChangeTotal > pcrSummary.PutOiChangeTotal * 1.2m) oiScore -= 8m;

        // Max Pain distance gravity
        if (maxPain.MaxPainStrike > 0)
        {
            var diff = spot - maxPain.MaxPainStrike;
            if (diff > 0 && pcrScore > 0) oiScore += 5m; // Spot holding above max pain with bullish PCR
            else if (diff < 0 && pcrScore < 0) oiScore -= 5m;
        }
        oiScore = Math.Clamp(oiScore, -25m, 25m);

        // 4. Pillar 3: Option Greeks & IV Regime Score (Max 25 pts)
        decimal greeksScore = 0;
        var atmStrikeObj = optionChain.Strikes.FirstOrDefault(s => s.IsAtm) 
            ?? optionChain.Strikes.OrderBy(s => Math.Abs(s.StrikePrice - spot)).FirstOrDefault();

        decimal atmCallDelta = atmStrikeObj?.Call?.Greeks?.Delta ?? 0.50m;
        decimal atmPutDelta = atmStrikeObj?.Put?.Greeks?.Delta ?? -0.50m;
        decimal atmIv = atmStrikeObj?.Call?.Greeks?.Iv ?? 14.0m;

        // Delta bias: If Call Delta is > 0.52 (spot trending higher into call side)
        if (atmCallDelta >= 0.53m) greeksScore += 10m;
        else if (atmCallDelta <= 0.47m) greeksScore -= 10m;

        // IV Regime
        if (atmIv >= 11m && atmIv <= 18m)
        {
            // Stable IV favors trend continuation
            if (pcrScore > 0) greeksScore += 10m;
            else if (pcrScore < 0) greeksScore -= 10m;
        }
        else if (atmIv > 24m)
        {
            // High IV: volatile swings, slight compression penalty
            greeksScore -= 5m;
        }
        greeksScore = Math.Clamp(greeksScore, -25m, 25m);

        // 5. Pillar 4: Spot Trend & Momentum Score (Max 25 pts)
        decimal trendScore = 0;
        if (spot > tech.Ema9 && tech.Ema9 > tech.Ema21) trendScore += 12m;
        else if (spot < tech.Ema9 && tech.Ema9 < tech.Ema21) trendScore -= 12m;

        if (tech.IsSupertrendBullish) trendScore += 6m;
        else trendScore -= 6m;

        if (tech.Rsi14 >= 52m && tech.Rsi14 <= 68m) trendScore += 7m;
        else if (tech.Rsi14 <= 48m && tech.Rsi14 >= 32m) trendScore -= 7m;
        else if (tech.Rsi14 > 72m) trendScore -= 4m; // Overbought hesitation
        else if (tech.Rsi14 < 28m) trendScore += 4m; // Oversold hesitation

        trendScore = Math.Clamp(trendScore, -25m, 25m);

        // 6. Aggregate Total Score (-100 to +100)
        var totalScore = Math.Clamp(pcrScore + oiScore + greeksScore + trendScore, -100m, 100m);

        // 7. Recommendation and Recommended Strike Selection
        StrategyRecommendation recommendation;
        string setupRationale;
        string recommendedStrike = string.Empty;
        OptionType? recommendedOptionType = null;
        TransactionType recommendedAction = TransactionType.BUY;
        decimal entryPrice = 0m;
        decimal stopLoss = 0m;
        decimal target = 0m;

        var atmStrikePrice = atmStrikeObj?.StrikePrice ?? spot;

        if (totalScore >= 60m)
        {
            recommendation = StrategyRecommendation.StrongBuyCall;
            setupRationale = $"High Conviction Bullish Confluence (Score: +{totalScore:F0}). Heavy Put writing (PCR {oiPcr:F2}), Spot > EMA 9/21, and positive Delta skew.";
            recommendedOptionType = OptionType.CE;
            recommendedAction = TransactionType.BUY;

            // Pick ATM Call or slight OTM Call for high gamma burst
            var callCandidate = atmStrikeObj?.Call;
            recommendedStrike = $"{atmStrikePrice:F0} CE";
            entryPrice = callCandidate?.Ltp ?? 100m;
            stopLoss = Math.Round(entryPrice * 0.75m, 2); // 25% Stop Loss
            target = Math.Round(entryPrice * 1.45m, 2);   // 45% Target
        }
        else if (totalScore <= -60m)
        {
            recommendation = StrategyRecommendation.StrongBuyPut;
            setupRationale = $"High Conviction Bearish Confluence (Score: {totalScore:F0}). Heavy Call writing (PCR {oiPcr:F2}), Spot < EMA 9/21, and negative Delta bias.";
            recommendedOptionType = OptionType.PE;
            recommendedAction = TransactionType.BUY;

            // Pick ATM Put
            var putCandidate = atmStrikeObj?.Put;
            recommendedStrike = $"{atmStrikePrice:F0} PE";
            entryPrice = putCandidate?.Ltp ?? 100m;
            stopLoss = Math.Round(entryPrice * 0.75m, 2); // 25% Stop Loss
            target = Math.Round(entryPrice * 1.45m, 2);   // 45% Target
        }
        else if (totalScore >= 30m)
        {
            recommendation = StrategyRecommendation.Neutral;
            setupRationale = $"Mild Bullish Bias (Score: +{totalScore:F0}), waiting for confirmation above +60 before triggering execution.";
            recommendedStrike = $"{atmStrikePrice:F0} CE";
            recommendedOptionType = OptionType.CE;
        }
        else if (totalScore <= -30m)
        {
            recommendation = StrategyRecommendation.Neutral;
            setupRationale = $"Mild Bearish Bias (Score: {totalScore:F0}), waiting for breakdown below -60 before triggering execution.";
            recommendedStrike = $"{atmStrikePrice:F0} PE";
            recommendedOptionType = OptionType.PE;
        }
        else
        {
            recommendation = StrategyRecommendation.RangeboundHold;
            setupRationale = $"Rangebound / Choppy Market (Score: {totalScore:F0}). High Theta decay zone near Max Pain ({maxPain.MaxPainStrike:F0}). Avoid naked directional options.";
            recommendedStrike = $"{atmStrikePrice:F0} Straddle";
        }

        return Task.FromResult(new FnOStrategyScoreDto
        {
            IndexSymbol = optionChain.UnderlyingSymbol,
            SpotPrice = spot,
            TotalScore = totalScore,
            PcrScore = pcrScore,
            OiMaxPainScore = oiScore,
            GreeksIvScore = greeksScore,
            SpotTrendScore = trendScore,
            Recommendation = recommendation,
            SetupRationale = setupRationale,
            RecommendedStrike = recommendedStrike,
            RecommendedOptionType = recommendedOptionType,
            RecommendedAction = recommendedAction,
            RecommendedEntryPrice = entryPrice,
            RecommendedStopLoss = stopLoss,
            RecommendedTarget = target,
            ExpectedDelta = recommendedOptionType == OptionType.CE ? atmCallDelta : atmPutDelta,
            ExpectedThetaDecay = atmStrikeObj?.Call?.Greeks?.Theta ?? 0m,
            CalculatedAtUtc = DateTime.UtcNow
        });
    }
}
