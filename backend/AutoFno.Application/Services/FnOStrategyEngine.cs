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

        // 1. Spot Technical Indicators & Pillar 4: Spot Trend & Momentum Score (Max 25 pts)
        var tech = SpotIndicatorCalculator.Calculate(spotCandles);

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

        // 2. Pillar 1: PCR Sentiment Score (Max 25 pts)
        // Continuous, sensitive quantitative model around Indian Index 1.00 baseline
        decimal pcrScore = 0;
        var oiPcr = pcrSummary.OiPcr;

        if (oiPcr >= 1.00m)
        {
            // Bullish Put writing floor dominance: scales smoothly up to +20 pts
            pcrScore = Math.Min(20m, (oiPcr - 1.00m) * 40m);
            if (oiPcr >= 1.45m && oiPcr <= 1.65m) pcrScore = 22m;
            else if (oiPcr > 1.65m) pcrScore = 14m; // Overbought exhaustion fade penalty
        }
        else
        {
            // Bearish Call writing ceiling dominance: scales smoothly down to -20 pts
            pcrScore = Math.Max(-20m, (oiPcr - 1.00m) * 40m);
            if (oiPcr <= 0.55m && oiPcr >= 0.40m) pcrScore = -22m;
            else if (oiPcr < 0.40m) pcrScore = -14m; // Oversold short-squeeze bounce risk
        }

        // Intraday Volume PCR Confluence / Divergence (Dynamic, independent of pcrScore)
        if (pcrSummary.VolumePcr >= 1.10m) pcrScore += 3m;
        else if (pcrSummary.VolumePcr <= 0.90m) pcrScore -= 3m;

        // ATM PCR Sentiment (Immediate near-the-money options battleground)
        if (pcrSummary.AtmPcr >= 1.05m) pcrScore += 2m;
        else if (pcrSummary.AtmPcr <= 0.95m) pcrScore -= 2m;

        // PCR Slope / Velocity (Is PCR expanding or contracting intraday?)
        if (pcrSummary.PcrSlope > 0.015m) pcrScore += 2m;
        else if (pcrSummary.PcrSlope < -0.015m) pcrScore -= 2m;

        pcrScore = Math.Clamp(Math.Round(pcrScore, 1), -25m, 25m);

        // 3. Pillar 2: Open Interest & Max Pain Gravity Score (Max 25 pts)
        decimal oiScore = 0;
        var totalBullishBuildup = pcrSummary.LongBuildupCount + pcrSummary.ShortCoveringCount;
        var totalBearishBuildup = pcrSummary.ShortBuildupCount + pcrSummary.LongUnwindingCount;
        var netBuildup = totalBullishBuildup - totalBearishBuildup;

        if (netBuildup > 3) oiScore += 12m;
        else if (netBuildup < -3) oiScore -= 12m;
        else oiScore += netBuildup * 2m; // Micro buildup advantage

        // Fresh OI addition (Put Writing vs Call Writing)
        if (pcrSummary.PutOiChangeTotal > pcrSummary.CallOiChangeTotal * 1.15m) oiScore += 8m;
        else if (pcrSummary.CallOiChangeTotal > pcrSummary.PutOiChangeTotal * 1.15m) oiScore -= 8m;

        // Max Pain distance gravity
        if (maxPain.MaxPainStrike > 0)
        {
            var diff = spot - maxPain.MaxPainStrike;
            if (diff > 5m) oiScore += 5m; // Spot holding above max pain (bullish drift)
            else if (diff < -5m) oiScore -= 5m; // Spot pinned below max pain (bearish drift)
        }
        oiScore = Math.Clamp(Math.Round(oiScore, 1), -25m, 25m);

        // 4. Pillar 3: Option Greeks & IV Regime Score (Max 25 pts)
        // Decoupled from pcrScore: directly evaluates Delta asymmetry, IV regime, Volatility Skew & Gamma
        decimal greeksScore = 0;
        var atmStrikeObj = optionChain.Strikes.FirstOrDefault(s => s.IsAtm) 
            ?? optionChain.Strikes.OrderBy(s => Math.Abs(s.StrikePrice - spot)).FirstOrDefault();

        decimal atmCallDelta = atmStrikeObj?.Call?.Greeks?.Delta ?? 0.50m;
        decimal atmPutDelta = atmStrikeObj?.Put?.Greeks?.Delta ?? -0.50m;
        decimal atmCallIv = atmStrikeObj?.Call?.Greeks?.Iv ?? 14.0m;
        decimal atmPutIv = atmStrikeObj?.Put?.Greeks?.Iv ?? 14.0m;
        decimal atmIv = (atmCallIv + atmPutIv) / 2m;
        if (atmIv <= 0) atmIv = 14.0m;

        // 4A. Delta Asymmetry & Directional Edge (Max +/- 10 pts)
        // If Call Delta is above 0.50, spot has pushed above ATM strike center into call ITM territory
        var deltaSkew = atmCallDelta - 0.50m;
        if (atmCallDelta >= 0.52m) greeksScore += 8m;
        else if (atmCallDelta <= 0.48m) greeksScore -= 8m;
        else greeksScore += Math.Round(deltaSkew * 100m, 1); // Linear micro-tilt between 0.48 and 0.52

        // 4B. IV Regime & Volatility Environment (Max +/- 10 pts)
        // Grounded in spot momentum & market volatility state
        if (atmIv >= 11m && atmIv <= 18m)
        {
            // Stable / Low-to-Medium IV environment: ideal for directional trend continuation
            if (trendScore > 0 || tech.IsSupertrendBullish) greeksScore += 8m;
            else if (trendScore < 0 || !tech.IsSupertrendBullish) greeksScore -= 8m;
            else greeksScore += 2m; // Stable environment baseline
        }
        else if (atmIv > 22m)
        {
            // High IV regime: Options expensive, elevated IV crush risk
            greeksScore -= 6m;
        }
        else if (atmIv < 11m)
        {
            // Compressed low IV: Options cheap, primed for breakout volatility expansion
            if (trendScore >= 0) greeksScore += 6m;
            else greeksScore -= 6m;
        }

        // 4C. IV Volatility Skew (Call IV vs Put IV) (Max +/- 5 pts)
        // If Put IV > Call IV by > 0.8%, put hedging / fear pricing creates bearish skew
        var ivSkew = atmCallIv - atmPutIv;
        if (ivSkew >= 0.8m) greeksScore += 4m; // Call skew (bullish speculative bidding)
        else if (ivSkew <= -0.8m) greeksScore -= 4m; // Put skew (downside fear pricing)

        // 4D. Gamma Acceleration
        decimal atmGamma = atmStrikeObj?.Call?.Greeks?.Gamma ?? 0.0003m;
        if (atmGamma >= 0.00045m)
        {
            if (greeksScore > 0) greeksScore += 3m;
            else if (greeksScore < 0) greeksScore -= 3m;
        }

        greeksScore = Math.Clamp(Math.Round(greeksScore, 1), -25m, 25m);

        // 5. Aggregate Total Score (-100 to +100)
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
