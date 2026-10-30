using System;
using System.Collections.Generic;
using System.Linq;
using AutoFno.Application.Services.Mathematics;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class SpotTrendEngine : ISpotTrendEngine
{
    public V2SpotTrendResult Analyze(List<Candle> spotCandles, decimal spotPrice, QuantitativeConvictionV2Config config)
    {
        var result = new V2SpotTrendResult();

        if (spotCandles == null || spotCandles.Count < 21)
        {
            return result;
        }

        var tech = SpotIndicatorCalculator.Calculate(spotCandles);
        var spot = spotPrice > 0 ? spotPrice : (spotCandles.LastOrDefault()?.Close ?? 0m);

        result.SpotPrice = spot;
        result.Ema9 = tech.Ema9;
        result.Ema21 = tech.Ema21;
        result.Rsi14 = tech.Rsi14;
        result.IsSupertrendBullish = tech.IsSupertrendBullish;

        // Calculate VWAP from candles
        decimal totalPv = 0m;
        long totalVol = 0;
        foreach (var c in spotCandles)
        {
            var typicalPrice = (c.High + c.Low + c.Close) / 3m;
            totalPv += typicalPrice * c.Volume;
            totalVol += c.Volume;
        }
        result.Vwap = totalVol > 0 ? Math.Round(totalPv / totalVol, 2) : spot;

        // Calculate 14-period ATR for normalization
        decimal sumTr = 0m;
        int atrPeriod = Math.Min(14, spotCandles.Count - 1);
        int startIndex = spotCandles.Count - atrPeriod;
        for (int i = startIndex; i < spotCandles.Count; i++)
        {
            var curr = spotCandles[i];
            var prev = spotCandles[i - 1];
            var tr = Math.Max(curr.High - curr.Low, Math.Max(Math.Abs(curr.High - prev.Close), Math.Abs(curr.Low - prev.Close)));
            sumTr += tr;
        }
        var atr = atrPeriod > 0 ? (sumTr / atrPeriod) : 10.0m;
        if (atr <= 0) atr = 10.0m;

        // ATR-normalized EMA separation: (Ema9 - Ema21) / ATR
        result.EmaSeparationAtrNormalized = Math.Round((tech.Ema9 - tech.Ema21) / atr, 2);

        // Overextension check: spot > Ema9 + 2.5*ATR or spot < Ema9 - 2.5*ATR
        result.IsOverextended = Math.Abs(spot - tech.Ema9) > (2.5m * atr);

        // Detect rangebound choppy compression: small candle range or tangled EMAs
        var recentCandles = spotCandles.Skip(Math.Max(0, spotCandles.Count - 11)).Take(10).ToList();
        var isCompressedRange = (recentCandles.Count > 0 && (recentCandles.Max(c => c.High) - recentCandles.Min(c => c.Low)) <= (atr * 1.5m))
                                || Math.Abs(tech.Ema9 - tech.Ema21) < 1.0m;

        // Sub-component 1: EMA 9/21 Alignment and Slope (Max 8 pts)
        decimal emaScore = 0m;
        if (!isCompressedRange)
        {
            if (spot > tech.Ema9 && tech.Ema9 > tech.Ema21)
            {
                emaScore = config.EmaAlignmentSlopeWeight; // Full bullish alignment: +8
            }
            else if (spot < tech.Ema9 && tech.Ema9 < tech.Ema21)
            {
                emaScore = -config.EmaAlignmentSlopeWeight; // Full bearish alignment: -8
            }
            else if (spot > tech.Ema21)
            {
                emaScore = 3.0m;
            }
            else if (spot < tech.Ema21)
            {
                emaScore = -3.0m;
            }
        }
        result.EmaAlignmentSlopeScore = Math.Clamp(Math.Round(emaScore, 1), -config.EmaAlignmentSlopeWeight, config.EmaAlignmentSlopeWeight);

        // Sub-component 2: Supertrend Direction (Max 5 pts)
        decimal stScore = 0m;
        if (!isCompressedRange)
        {
            stScore = tech.IsSupertrendBullish ? config.SupertrendDirectionWeight : -config.SupertrendDirectionWeight;
        }
        result.SupertrendDirectionScore = Math.Clamp(Math.Round(stScore, 1), -config.SupertrendDirectionWeight, config.SupertrendDirectionWeight);

        // Sub-component 3: RSI-14 Momentum Regime (Max 4 pts)
        decimal rsiScore = 0m;
        if (tech.Rsi14 >= 55.0m)
        {
            rsiScore = config.RsiMomentumRegimeWeight; // Bullish momentum: +4
        }
        else if (tech.Rsi14 <= 45.0m)
        {
            rsiScore = -config.RsiMomentumRegimeWeight; // Bearish momentum: -4
        }
        else
        {
            rsiScore = 0m; // Neutral consolidation: 0
        }
        result.RsiMomentumRegimeScore = Math.Clamp(Math.Round(rsiScore, 1), -config.RsiMomentumRegimeWeight, config.RsiMomentumRegimeWeight);

        // Sub-component 4: VWAP and Price Structure (Max 8 pts)
        decimal vwapScore = 0m;
        if (!isCompressedRange && result.Vwap > 0)
        {
            if (spot > result.Vwap) vwapScore += 4.0m;
            else if (spot < result.Vwap) vwapScore -= 4.0m;

            if (recentCandles.Count > 0)
            {
                var swingHigh = recentCandles.Max(c => c.High);
                var swingLow = recentCandles.Min(c => c.Low);

                if (spot >= swingHigh) vwapScore += 4.0m;
                else if (spot <= swingLow) vwapScore -= 4.0m;
            }
        }
        result.VwapPriceStructureScore = Math.Clamp(Math.Round(vwapScore, 1), -config.VwapPriceStructureWeight, config.VwapPriceStructureWeight);

        // Aggregate Pillar 4 Score (-25 to +25)
        result.TotalSpotPillarScore = Math.Clamp(
            result.EmaAlignmentSlopeScore + result.SupertrendDirectionScore + result.RsiMomentumRegimeScore + result.VwapPriceStructureScore,
            -25.0m,
            25.0m);

        // Classify Market Regime
        if (result.TotalSpotPillarScore >= 18.0m) result.Regime = MarketRegime.StrongBullTrend;
        else if (result.TotalSpotPillarScore >= 8.0m) result.Regime = MarketRegime.MildBullBias;
        else if (result.TotalSpotPillarScore <= -18.0m) result.Regime = MarketRegime.StrongBearTrend;
        else if (result.TotalSpotPillarScore <= -8.0m) result.Regime = MarketRegime.MildBearBias;
        else result.Regime = MarketRegime.RangeboundChoppy;

        return result;
    }
}
