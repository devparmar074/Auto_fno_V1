using System;
using System.Collections.Generic;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Xunit;

namespace AutoFno.Tests.V2;

public class Pillar1PCRTests
{
    private readonly PCRAnalysisEngine _engine = new();
    private readonly QuantitativeConvictionV2Config _config = new();

    [Theory]
    [InlineData(1.8, 10.0)]   // Extremely bullish PCR
    [InlineData(1.4, 6.0)]    // Bullish PCR
    [InlineData(1.0, 0.0)]    // Neutral PCR
    [InlineData(0.7, -4.0)]   // Bearish PCR
    [InlineData(0.4, -10.0)]  // Extremely bearish PCR
    public void PCR_Level_Maps_To_Expected_Score(decimal pcrValue, decimal expectedMinScore)
    {
        var pcr = new PcrSummaryDto
        {
            OiPcr = pcrValue,
            VolumePcr = pcrValue,
            AtmPcr = pcrValue,
            PrevOiPcr = pcrValue,
            PcrSlope = 0m,
            Sentiment = pcrValue > 1.2m ? "Bullish" : (pcrValue < 0.8m ? "Bearish" : "Neutral")
        };
        var chain = new OptionChainDto { Strikes = new List<StrikeDataDto>() };

        var result = _engine.Analyze(chain, pcr, _config);

        if (expectedMinScore > 0)
            Assert.True(result.VolumePcrFlowScore >= expectedMinScore, $"Expected >= {expectedMinScore}, got {result.VolumePcrFlowScore}");
        else if (expectedMinScore < 0)
            Assert.True(result.VolumePcrFlowScore <= expectedMinScore, $"Expected <= {expectedMinScore}, got {result.VolumePcrFlowScore}");
        else
            Assert.Equal(0m, result.VolumePcrFlowScore);
    }

    [Fact]
    public void PCR_Total_Score_Never_Exceeds_Boundaries_On_Extreme_Values()
    {
        // Test extreme values
        decimal[] extremes = { 0.0001m, 0.1m, 0.2m, 2.5m, 5.0m, 50.0m };
        var chain = new OptionChainDto { Strikes = new List<StrikeDataDto>() };

        foreach (var pcrVal in extremes)
        {
            var pcr = new PcrSummaryDto
            {
                OiPcr = pcrVal,
                VolumePcr = pcrVal,
                AtmPcr = pcrVal,
                PrevOiPcr = pcrVal > 1.0m ? pcrVal - 0.5m : pcrVal + 0.5m,
                PcrSlope = pcrVal > 1.0m ? 0.5m : -0.5m,
                Sentiment = pcrVal > 1.0m ? "Bullish" : "Bearish"
            };

            var result = _engine.Analyze(chain, pcr, _config);

            Assert.InRange(result.TotalPcrPillarScore, -25.0m, 25.0m);
            Assert.InRange(result.VolumePcrFlowScore, -10.0m, 10.0m);
            Assert.InRange(result.AtmPcrScore, -7.0m, 7.0m);
            Assert.InRange(result.PcrSlopeVelocityScore, -5.0m, 5.0m);
            Assert.InRange(result.BroaderOiPcrScore, -3.0m, 3.0m);
        }
    }

    [Fact]
    public void PCR_Slope_Accurately_Detects_Rising_Sentiment()
    {
        var pcr = new PcrSummaryDto
        {
            OiPcr = 1.35m,
            VolumePcr = 1.25m,
            AtmPcr = 1.30m,
            PrevOiPcr = 1.15m,
            PcrSlope = 0.20m, // +0.20 delta over 30 mins
            Sentiment = "Bullish"
        };
        var chain = new OptionChainDto { Strikes = new List<StrikeDataDto>() };

        var result = _engine.Analyze(chain, pcr, _config);

        Assert.True(result.PcrSlopeVelocityScore > 0, "Rising PCR should yield positive slope score");
        Assert.True(result.TotalPcrPillarScore > 0, "Bullish PCR setup should yield positive pillar score");
    }
}
