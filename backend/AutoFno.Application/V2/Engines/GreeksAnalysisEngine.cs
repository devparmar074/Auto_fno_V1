using System;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class GreeksAnalysisEngine : IGreeksAnalysisEngine
{
    public V2GreeksAnalysisResult Analyze(OptionChainDto optionChain, decimal spotPrice, QuantitativeConvictionV2Config config)
    {
        var result = new V2GreeksAnalysisResult();

        var spot = spotPrice > 0 ? spotPrice : optionChain.SpotPrice;
        if (spot <= 0 || optionChain.Strikes == null || optionChain.Strikes.Count == 0)
        {
            return result;
        }

        // Find ATM strike
        var atmStrikeObj = optionChain.Strikes.FirstOrDefault(s => s.IsAtm)
            ?? optionChain.Strikes.OrderBy(s => Math.Abs(s.StrikePrice - spot)).FirstOrDefault();

        if (atmStrikeObj == null)
        {
            return result;
        }

        result.AtmDeltaCe = atmStrikeObj.Call?.Greeks?.Delta ?? 0.50m;
        result.AtmDeltaPe = atmStrikeObj.Put?.Greeks?.Delta ?? -0.50m;
        result.AtmGamma = atmStrikeObj.Call?.Greeks?.Gamma ?? 0.0004m;
        result.AtmTheta = atmStrikeObj.Call?.Greeks?.Theta ?? -15.0m;
        result.AtmVega = atmStrikeObj.Call?.Greeks?.Vega ?? 5.0m;

        var callIv = atmStrikeObj.Call?.Greeks?.Iv ?? 14.0m;
        var putIv = atmStrikeObj.Put?.Greeks?.Iv ?? 14.0m;
        result.AtmIv = (callIv + putIv) / 2m;
        result.IvSkew = callIv - putIv;

        // Classify IV Environment
        if (result.AtmIv < 11.0m) result.IvEnvironment = IvEnvironment.LowCompressed;
        else if (result.AtmIv <= 18.0m) result.IvEnvironment = IvEnvironment.StableNormal;
        else if (result.AtmIv <= 24.0m) result.IvEnvironment = IvEnvironment.ElevatedHigh;
        else result.IvEnvironment = IvEnvironment.CrushRisk;

        // Sub-component 1: ATM Delta Asymmetry (Max 6 pts)
        // Neutral baseline is Delta = 0.50 for CE, -0.50 for PE.
        // If Call Delta > 0.50, underlying has shifted ITM for calls (bullish momentum).
        var deltaSkew = result.AtmDeltaCe - 0.50m;
        decimal deltaScore = 0m;
        if (deltaSkew > 0.01m)
        {
            deltaScore = Math.Min(config.AtmDeltaAsymmetryWeight, deltaSkew * 100m);
        }
        else if (deltaSkew < -0.01m)
        {
            deltaScore = Math.Max(-config.AtmDeltaAsymmetryWeight, deltaSkew * 100m);
        }
        result.AtmDeltaAsymmetryScore = Math.Clamp(Math.Round(deltaScore, 1), -config.AtmDeltaAsymmetryWeight, config.AtmDeltaAsymmetryWeight);

        // Sub-component 2: IV Regime and Momentum (Max 7 pts)
        // Low/Stable IV supports directional trends. Extreme high IV carries crush penalty.
        decimal ivScore = 0m;
        switch (result.IvEnvironment)
        {
            case IvEnvironment.StableNormal:
                // Normal volatility supports steady directional trend continuation
                ivScore = (result.AtmDeltaAsymmetryScore >= 0) ? 5.0m : -5.0m;
                break;
            case IvEnvironment.LowCompressed:
                // Low compressed IV: Options are cheap, high gamma/delta burst potential
                ivScore = (result.AtmDeltaAsymmetryScore >= 0) ? config.IvRegimeMomentumWeight : -config.IvRegimeMomentumWeight;
                break;
            case IvEnvironment.ElevatedHigh:
                // Elevated IV: Moderate crush penalty
                ivScore = -3.0m;
                break;
            case IvEnvironment.CrushRisk:
                // High IV: Severe volatility crush penalty for long options
                ivScore = -config.IvRegimeMomentumWeight;
                break;
        }
        result.IvRegimeMomentumScore = Math.Clamp(Math.Round(ivScore, 1), -config.IvRegimeMomentumWeight, config.IvRegimeMomentumWeight);

        // Sub-component 3: Call/Put IV Skew (Max 5 pts)
        // Call IV > Put IV indicates upside speculative bidding (Bullish skew)
        // Put IV > Call IV indicates downside hedging/fear pricing (Bearish skew)
        decimal skewScore = 0m;
        if (result.IvSkew > 0.5m)
        {
            skewScore = Math.Min(config.CallPutIvSkewWeight, result.IvSkew * 2.5m);
        }
        else if (result.IvSkew < -0.5m)
        {
            skewScore = Math.Max(-config.CallPutIvSkewWeight, result.IvSkew * 2.5m);
        }
        result.CallPutIvSkewScore = Math.Clamp(Math.Round(skewScore, 1), -config.CallPutIvSkewWeight, config.CallPutIvSkewWeight);

        // Sub-component 4: Gamma versus Theta Efficiency (Max 7 pts)
        // Ratio of Gamma acceleration power to daily Theta decay bleed
        decimal efficiencyScore = 0m;
        var absTheta = Math.Abs(result.AtmTheta);
        if (absTheta > 0)
        {
            // Expected gamma acceleration over a 25 pt spot move vs theta cost per 4 hours (0.17 day)
            var gammaBenefit = 0.5m * result.AtmGamma * 25m * 25m; // 0.5 * Gamma * dS^2
            var intradayThetaBleed = absTheta * 0.17m; // ~4 hours holding
            if (gammaBenefit >= intradayThetaBleed)
            {
                efficiencyScore = config.GammaThetaEfficiencyWeight;
            }
            else
            {
                var ratio = intradayThetaBleed > 0 ? gammaBenefit / intradayThetaBleed : 0m;
                efficiencyScore = Math.Max(-config.GammaThetaEfficiencyWeight, (ratio - 1.0m) * 7.0m);
            }
        }
        result.GammaThetaEfficiencyScore = Math.Clamp(Math.Round(efficiencyScore, 1), -config.GammaThetaEfficiencyWeight, config.GammaThetaEfficiencyWeight);

        // Aggregate Pillar 3 Score (-25 to +25)
        result.TotalGreeksPillarScore = Math.Clamp(
            result.AtmDeltaAsymmetryScore + result.IvRegimeMomentumScore + result.CallPutIvSkewScore + result.GammaThetaEfficiencyScore,
            -25.0m,
            25.0m);

        // Option Premium Movement Estimation Model (Taylor Expansion)
        // Assuming expected underlying move dS = 30 pts over dt = 0.10 days (~2.5 hours) with dIV = -0.5%
        decimal dS = 30.0m;
        decimal dt = 0.10m;
        decimal dIv = -0.5m;

        // CE Expected Move
        result.EstimatedPremiumChangeCe = Math.Round(
            (result.AtmDeltaCe * dS) 
            + (0.5m * result.AtmGamma * dS * dS) 
            + (result.AtmVega * dIv) 
            + (result.AtmTheta * dt), 
            2);

        // PE Expected Move (Underlying drops by dS = -30 pts)
        result.EstimatedPremiumChangePe = Math.Round(
            (result.AtmDeltaPe * (-dS)) 
            + (0.5m * result.AtmGamma * (-dS) * (-dS)) 
            + (result.AtmVega * dIv) 
            + (result.AtmTheta * dt), 
            2);

        result.IsExpectedPremiumMoveSufficient = 
            (result.EstimatedPremiumChangeCe >= config.MinExpectedPremiumMovement) ||
            (result.EstimatedPremiumChangePe >= config.MinExpectedPremiumMovement);

        return result;
    }

    public decimal EstimateOptionPremiumMovement(decimal dS, decimal delta, decimal gamma, decimal theta, decimal vega, decimal dIv, decimal dtDays)
    {
        return Math.Round((delta * dS) + (0.5m * gamma * dS * dS) + (vega * dIv) + (theta * dtDays), 2);
    }
}
