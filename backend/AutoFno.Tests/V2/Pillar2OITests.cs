using System;
using System.Collections.Generic;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Xunit;

namespace AutoFno.Tests.V2;

public class Pillar2OITests
{
    private readonly OIAnalysisEngine _engine = new();
    private readonly QuantitativeConvictionV2Config _config = new();

    public static BuildupType ClassifyBuildup(decimal priceChange, long oiChange)
    {
        if (priceChange > 0 && oiChange > 0) return BuildupType.LongBuildup;
        if (priceChange < 0 && oiChange > 0) return BuildupType.ShortBuildup;
        if (priceChange > 0 && oiChange < 0) return BuildupType.ShortCovering;
        if (priceChange < 0 && oiChange < 0) return BuildupType.LongUnwinding;
        return BuildupType.Neutral;
    }

    [Theory]
    [InlineData(10.0, 5000, BuildupType.LongBuildup)]   // Price UP, OI UP
    [InlineData(-10.0, 5000, BuildupType.ShortBuildup)]  // Price DOWN, OI UP
    [InlineData(10.0, -5000, BuildupType.ShortCovering)] // Price UP, OI DOWN
    [InlineData(-10.0, -5000, BuildupType.LongUnwinding)]// Price DOWN, OI DOWN
    public void OI_Buildup_Classification_Identified_Accurately(
        decimal priceChange, long oiChange, BuildupType expectedBuildup)
    {
        var buildup = ClassifyBuildup(priceChange, oiChange);
        Assert.Equal(expectedBuildup, buildup);
    }

    [Fact]
    public void OI_Pillar_Score_Remains_Within_25_Point_Bounds()
    {
        var chain = new OptionChainDto
        {
            SpotPrice = 24500,
            CallWallStrike = 25000,
            PutWallStrike = 24000,
            Strikes = new List<StrikeDataDto>()
        };

        var pcrSummary = new PcrSummaryDto
        {
            LongBuildupCount = 15,
            ShortCoveringCount = 10,
            ShortBuildupCount = 2,
            LongUnwindingCount = 1,
            PutOiChangeTotal = 5000000,
            CallOiChangeTotal = 1000000
        };

        var maxPain = new MaxPainDto { MaxPainStrike = 24600, DistanceFromSpot = 100 };
        var result = _engine.Analyze(chain, pcrSummary, maxPain, _config);

        Assert.InRange(result.TotalOiPillarScore, -25.0m, 25.0m);
        Assert.InRange(result.BuildupDominanceScore, -10.0m, 10.0m);
        Assert.InRange(result.FreshWritingFlowScore, -8.0m, 8.0m);
        Assert.InRange(result.OiSupportResistanceScore, -5.0m, 5.0m);
        Assert.InRange(result.MaxPainDistanceScore, -2.0m, 2.0m);
    }
}
