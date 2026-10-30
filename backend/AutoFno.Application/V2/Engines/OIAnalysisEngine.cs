using System;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class OIAnalysisEngine : IOIAnalysisEngine
{
    public V2OiAnalysisResult Analyze(
        OptionChainDto optionChain,
        PcrSummaryDto pcrSummary,
        MaxPainDto maxPain,
        QuantitativeConvictionV2Config config)
    {
        var result = new V2OiAnalysisResult
        {
            LongBuildupCount = pcrSummary.LongBuildupCount,
            ShortBuildupCount = pcrSummary.ShortBuildupCount,
            ShortCoveringCount = pcrSummary.ShortCoveringCount,
            LongUnwindingCount = pcrSummary.LongUnwindingCount,
            CallWallStrike = optionChain.CallWallStrike,
            PutWallStrike = optionChain.PutWallStrike,
            MaxPainStrike = maxPain.MaxPainStrike,
            PutOiAdditionTotal = pcrSummary.PutOiChangeTotal,
            CallOiAdditionTotal = pcrSummary.CallOiChangeTotal
        };

        var spot = optionChain.SpotPrice;

        // Sub-component 1: Buildup Dominance (Max 10 pts)
        // Bullish Buildup = Long Buildup (calls) + Short Covering (calls/puts)
        // Bearish Buildup = Short Buildup (calls/puts) + Long Unwinding (calls)
        var totalBullish = pcrSummary.LongBuildupCount + pcrSummary.ShortCoveringCount;
        var totalBearish = pcrSummary.ShortBuildupCount + pcrSummary.LongUnwindingCount;
        result.NetBuildup = totalBullish - totalBearish;

        decimal buildupScore = 0m;
        if (result.NetBuildup > 0)
        {
            buildupScore = Math.Min(config.BuildupDominanceWeight, result.NetBuildup * 1.5m);
        }
        else if (result.NetBuildup < 0)
        {
            buildupScore = Math.Max(-config.BuildupDominanceWeight, result.NetBuildup * 1.5m);
        }
        result.BuildupDominanceScore = Math.Clamp(Math.Round(buildupScore, 1), -config.BuildupDominanceWeight, config.BuildupDominanceWeight);

        // Sub-component 2: Fresh Call/Put Writing Flow (Max 8 pts)
        // Fresh put writing (Put OI Change > Call OI Change) indicates strong support building
        // Fresh call writing (Call OI Change > Put OI Change) indicates resistance capping
        decimal writingScore = 0m;
        var putOiChange = pcrSummary.PutOiChangeTotal;
        var callOiChange = pcrSummary.CallOiChangeTotal;

        if (putOiChange > 0 || callOiChange > 0)
        {
            var totalOiChange = Math.Abs(putOiChange) + Math.Abs(callOiChange);
            if (totalOiChange > 0)
            {
                var netWritingRatio = (putOiChange - callOiChange) / totalOiChange;
                writingScore = netWritingRatio * config.FreshWritingFlowWeight;
            }
        }
        result.FreshWritingFlowScore = Math.Clamp(Math.Round(writingScore, 1), -config.FreshWritingFlowWeight, config.FreshWritingFlowWeight);

        // Sub-component 3: OI Support and Resistance Zones (Max 5 pts)
        // Distance to Call Wall (Resistance) and Put Wall (Support)
        decimal srScore = 0m;
        if (result.CallWallStrike > 0 && result.PutWallStrike > 0 && result.CallWallStrike > result.PutWallStrike)
        {
            var totalRange = result.CallWallStrike - result.PutWallStrike;
            if (totalRange > 0 && spot >= result.PutWallStrike && spot <= result.CallWallStrike)
            {
                // Position in channel: 0 = at support (bullish bounce zone), 1 = at resistance (bearish rejection zone)
                var positionInChannel = (spot - result.PutWallStrike) / totalRange;
                if (positionInChannel <= 0.30m)
                {
                    // Near Put Wall Support -> Potential bounce
                    srScore = (0.30m - positionInChannel) / 0.30m * config.OiSupportResistanceWeight;
                }
                else if (positionInChannel >= 0.70m)
                {
                    // Near Call Wall Resistance -> Potential rejection
                    srScore = -((positionInChannel - 0.70m) / 0.30m * config.OiSupportResistanceWeight);
                }
            }
            else if (spot > result.CallWallStrike)
            {
                // Breakout above Call Wall (Strong short covering squeeze potential)
                srScore = config.OiSupportResistanceWeight;
            }
            else if (spot < result.PutWallStrike)
            {
                // Breakdown below Put Wall (Long unwinding cascade potential)
                srScore = -config.OiSupportResistanceWeight;
            }
        }
        result.OiSupportResistanceScore = Math.Clamp(Math.Round(srScore, 1), -config.OiSupportResistanceWeight, config.OiSupportResistanceWeight);

        // Sub-component 4: Max Pain Distance (Max 2 pts)
        // Subtle pinning gravity towards Max Pain strike
        decimal painScore = 0m;
        if (maxPain.MaxPainStrike > 0 && spot > 0)
        {
            result.DistanceToMaxPain = spot - maxPain.MaxPainStrike;
            if (result.DistanceToMaxPain > 10m)
            {
                // Spot holding solidly above Max Pain
                painScore = config.MaxPainDistanceWeight;
            }
            else if (result.DistanceToMaxPain < -10m)
            {
                // Spot holding solidly below Max Pain
                painScore = -config.MaxPainDistanceWeight;
            }
        }
        result.MaxPainDistanceScore = Math.Clamp(Math.Round(painScore, 1), -config.MaxPainDistanceWeight, config.MaxPainDistanceWeight);

        // Aggregate Pillar 2 Score (-25 to +25)
        result.TotalOiPillarScore = Math.Clamp(
            result.BuildupDominanceScore + result.FreshWritingFlowScore + result.OiSupportResistanceScore + result.MaxPainDistanceScore,
            -25.0m,
            25.0m);

        return result;
    }
}
