using System;
using System.Collections.Generic;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class ConvictionScoringEngine : IConvictionScoringEngineV2
{
    private readonly IPCRAnalysisEngine _pcrEngine;
    private readonly IOIAnalysisEngine _oiEngine;
    private readonly IGreeksAnalysisEngine _greeksEngine;
    private readonly ISpotTrendEngine _spotTrendEngine;

    public ConvictionScoringEngine(
        IPCRAnalysisEngine pcrEngine,
        IOIAnalysisEngine oiEngine,
        IGreeksAnalysisEngine greeksEngine,
        ISpotTrendEngine spotTrendEngine)
    {
        _pcrEngine = pcrEngine;
        _oiEngine = oiEngine;
        _greeksEngine = greeksEngine;
        _spotTrendEngine = spotTrendEngine;
    }

    public V2ConvictionScoreDto CalculateConvictionScore(
        OptionChainDto optionChain,
        PcrSummaryDto pcrSummary,
        MaxPainDto maxPain,
        List<Candle> spotCandles,
        QuantitativeConvictionV2Config config)
    {
        var spot = optionChain.SpotPrice > 0 ? optionChain.SpotPrice : (spotCandles.Count > 0 ? spotCandles[^1].Close : 0m);

        var pcrResult = _pcrEngine.Analyze(optionChain, pcrSummary, config);
        var oiResult = _oiEngine.Analyze(optionChain, pcrSummary, maxPain, config);
        var greeksResult = _greeksEngine.Analyze(optionChain, spot, config);
        var spotResult = _spotTrendEngine.Analyze(spotCandles, spot, config);

        return CalculateScoreFromPillars(optionChain.UnderlyingSymbol, spot, pcrResult, oiResult, greeksResult, spotResult, config);
    }

    public V2ConvictionScoreDto CalculateScoreFromPillars(
        string symbol,
        decimal spotPrice,
        V2PcrAnalysisResult pcrResult,
        V2OiAnalysisResult oiResult,
        V2GreeksAnalysisResult greeksResult,
        V2SpotTrendResult spotResult,
        QuantitativeConvictionV2Config config)
    {
        var total = pcrResult.TotalPcrPillarScore 
                  + oiResult.TotalOiPillarScore 
                  + greeksResult.TotalGreeksPillarScore 
                  + spotResult.TotalSpotPillarScore;

        var clampedTotal = Math.Clamp(Math.Round(total, 1), -100.0m, 100.0m);

        var candidate = TradeCandidateDecision.NoTrade;
        StrategyRecommendation rec;
        string rationale;

        if (clampedTotal >= config.BullishCandidateThreshold)
        {
            candidate = TradeCandidateDecision.BuyAtmCeCandidate;
            rec = StrategyRecommendation.StrongBuyCall;
            rationale = $"High Conviction Bullish Confluence (Score: +{clampedTotal:F0}). " +
                        $"PCR: +{pcrResult.TotalPcrPillarScore:F0}, OI: +{oiResult.TotalOiPillarScore:F0}, Greeks: +{greeksResult.TotalGreeksPillarScore:F0}, Trend: +{spotResult.TotalSpotPillarScore:F0}.";
        }
        else if (clampedTotal <= config.BearishCandidateThreshold)
        {
            candidate = TradeCandidateDecision.BuyAtmPeCandidate;
            rec = StrategyRecommendation.StrongBuyPut;
            rationale = $"High Conviction Bearish Confluence (Score: {clampedTotal:F0}). " +
                        $"PCR: {pcrResult.TotalPcrPillarScore:F0}, OI: {oiResult.TotalOiPillarScore:F0}, Greeks: {greeksResult.TotalGreeksPillarScore:F0}, Trend: {spotResult.TotalSpotPillarScore:F0}.";
        }
        else if (clampedTotal >= 30.0m)
        {
            rec = StrategyRecommendation.Neutral;
            rationale = $"Mild Bullish Bias (Score: +{clampedTotal:F0}). Insufficient confluence for directional execution (< {config.BullishCandidateThreshold:F0}).";
        }
        else if (clampedTotal <= -30.0m)
        {
            rec = StrategyRecommendation.Neutral;
            rationale = $"Mild Bearish Bias (Score: {clampedTotal:F0}). Insufficient confluence for directional execution (> {config.BearishCandidateThreshold:F0}).";
        }
        else
        {
            rec = StrategyRecommendation.RangeboundHold;
            rationale = $"Rangebound / Choppy Market (Score: {clampedTotal:F0}).";
        }

        return new V2ConvictionScoreDto
        {
            IndexSymbol = symbol,
            SpotPrice = spotPrice,
            TimestampUtc = DateTime.UtcNow,
            PcrAnalysis = pcrResult,
            OiAnalysis = oiResult,
            GreeksAnalysis = greeksResult,
            SpotTrend = spotResult,
            TotalScore = clampedTotal,
            Recommendation = rec,
            CandidateDecision = candidate,
            SetupRationale = rationale
        };
    }
}
