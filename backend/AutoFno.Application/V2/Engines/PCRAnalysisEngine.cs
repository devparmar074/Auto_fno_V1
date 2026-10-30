using System;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class PCRAnalysisEngine : IPCRAnalysisEngine
{
    public V2PcrAnalysisResult Analyze(OptionChainDto optionChain, PcrSummaryDto pcrSummary, QuantitativeConvictionV2Config config)
    {
        var result = new V2PcrAnalysisResult
        {
            OiPcr = pcrSummary.OiPcr,
            VolumePcr = pcrSummary.VolumePcr,
            AtmPcr = pcrSummary.AtmPcr,
            PcrSlope = pcrSummary.PcrSlope
        };

        // 1. Detect Put Writing vs Put Buying & Call Writing vs Call Buying evidence
        int putShortBuildup = 0; // Put price down, Put OI up -> Put Writing (Bullish)
        int putLongBuildup = 0;  // Put price up, Put OI up -> Put Buying (Bearish)
        int callShortBuildup = 0;// Call price down, Call OI up -> Call Writing (Bearish)
        int callLongBuildup = 0; // Call price up, Call OI up -> Call Buying (Bullish)

        if (optionChain.Strikes != null)
        {
            foreach (var s in optionChain.Strikes)
            {
                if (s.Put != null)
                {
                    if (s.Put.Buildup == BuildupType.ShortBuildup) putShortBuildup++;
                    else if (s.Put.Buildup == BuildupType.LongBuildup) putLongBuildup++;
                }

                if (s.Call != null)
                {
                    if (s.Call.Buildup == BuildupType.ShortBuildup) callShortBuildup++;
                    else if (s.Call.Buildup == BuildupType.LongBuildup) callLongBuildup++;
                }
            }
        }

        result.PutWritingEvidence = putShortBuildup > putLongBuildup;
        result.CallWritingEvidence = callShortBuildup > callLongBuildup;

        // Sub-component 1: Intraday Volume PCR Flow (Max 10 pts)
        // Evaluates active volume turnover aggression
        decimal volScore = 0m;
        var volPcr = pcrSummary.VolumePcr;
        if (volPcr > 1.0m)
        {
            volScore = Math.Min(config.VolumePcrFlowWeight, (volPcr - 1.0m) * 20m);
        }
        else if (volPcr < 1.0m)
        {
            volScore = Math.Max(-config.VolumePcrFlowWeight, (volPcr - 1.0m) * 20m);
        }

        // Adjust by writing vs buying confirmation
        if (volScore > 0 && result.PutWritingEvidence) volScore = Math.Min(config.VolumePcrFlowWeight, volScore + 2m);
        else if (volScore < 0 && result.CallWritingEvidence) volScore = Math.Max(-config.VolumePcrFlowWeight, volScore - 2m);
        result.VolumePcrFlowScore = Math.Clamp(Math.Round(volScore, 1), -config.VolumePcrFlowWeight, config.VolumePcrFlowWeight);

        // Sub-component 2: ATM PCR (Max 7 pts)
        // Concentrated near-the-money sentiment within immediate strike corridor
        decimal atmScore = 0m;
        var atmPcr = pcrSummary.AtmPcr;
        if (atmPcr > 1.0m)
        {
            atmScore = Math.Min(config.AtmPcrWeight, (atmPcr - 1.0m) * 14m);
        }
        else if (atmPcr < 1.0m)
        {
            atmScore = Math.Max(-config.AtmPcrWeight, (atmPcr - 1.0m) * 14m);
        }
        result.AtmPcrScore = Math.Clamp(Math.Round(atmScore, 1), -config.AtmPcrWeight, config.AtmPcrWeight);

        // Sub-component 3: PCR Slope and Velocity (Max 5 pts)
        // Measures intraday delta PCR / dt
        decimal slopeScore = 0m;
        var slope = pcrSummary.PcrSlope;
        if (slope > 0.01m)
        {
            slopeScore = Math.Min(config.PcrSlopeVelocityWeight, slope * 100m);
        }
        else if (slope < -0.01m)
        {
            slopeScore = Math.Max(-config.PcrSlopeVelocityWeight, slope * 100m);
        }
        result.PcrSlopeVelocityScore = Math.Clamp(Math.Round(slopeScore, 1), -config.PcrSlopeVelocityWeight, config.PcrSlopeVelocityWeight);

        // Sub-component 4: Broader OI PCR Context (Max 3 pts)
        // Macro cumulative OI baseline context
        decimal oiScore = 0m;
        var oiPcr = pcrSummary.OiPcr;
        if (oiPcr >= 1.25m) oiScore = config.BroaderOiPcrWeight;
        else if (oiPcr >= 1.05m) oiScore = 1.5m;
        else if (oiPcr <= 0.75m) oiScore = -config.BroaderOiPcrWeight;
        else if (oiPcr <= 0.95m) oiScore = -1.5m;
        result.BroaderOiPcrScore = Math.Clamp(Math.Round(oiScore, 1), -config.BroaderOiPcrWeight, config.BroaderOiPcrWeight);

        // Aggregate Pillar 1 Score (-25 to +25)
        result.TotalPcrPillarScore = Math.Clamp(
            result.VolumePcrFlowScore + result.AtmPcrScore + result.PcrSlopeVelocityScore + result.BroaderOiPcrScore,
            -25.0m,
            25.0m);

        result.Interpretation = result.TotalPcrPillarScore switch
        {
            >= 15.0m => "Strong Bullish PCR (Heavy Put Writing & Expanding Flow)",
            >= 6.0m => "Mild Bullish PCR (Put Writing Dominance)",
            <= -15.0m => "Strong Bearish PCR (Heavy Call Writing & Contracting Flow)",
            <= -6.0m => "Mild Bearish PCR (Call Writing Dominance)",
            _ => "Neutral / Balanced PCR Sentiment"
        };

        return result;
    }
}
