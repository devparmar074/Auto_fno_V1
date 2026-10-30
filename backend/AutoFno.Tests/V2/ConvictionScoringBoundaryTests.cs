using System;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.V2;
using Xunit;

namespace AutoFno.Tests.V2;

public class ConvictionScoringBoundaryTests
{
    private readonly ConvictionScoringEngine _engine = new(
        new PCRAnalysisEngine(),
        new OIAnalysisEngine(),
        new GreeksAnalysisEngine(),
        new SpotTrendEngine());

    private readonly QuantitativeConvictionV2Config _config = new();

    [Fact]
    public void Total_Conviction_Score_Correctly_Sums_Four_Pillars()
    {
        var pcr = new V2PcrAnalysisResult { TotalPcrPillarScore = 20.0m };
        var oi = new V2OiAnalysisResult { TotalOiPillarScore = 15.0m };
        var greeks = new V2GreeksAnalysisResult { TotalGreeksPillarScore = 18.0m };
        var spot = new V2SpotTrendResult { TotalSpotPillarScore = 22.0m };

        var result = _engine.CalculateScoreFromPillars("NIFTY50", 24500, pcr, oi, greeks, spot, _config);

        // 20 + 15 + 18 + 22 = 75
        Assert.Equal(75.0m, result.TotalScore);
        Assert.Equal(StrategyRecommendation.StrongBuyCall, result.Recommendation);
        Assert.Equal(TradeCandidateDecision.BuyAtmCeCandidate, result.CandidateDecision);
    }

    [Fact]
    public void Score_Boundary_Invariant_Holds_Across_10000_Randomized_Inputs()
    {
        var random = new Random(42);

        for (int i = 0; i < 10000; i++)
        {
            // Inject random scores ranging from -100 to +100 in each pillar
            var pcr = new V2PcrAnalysisResult { TotalPcrPillarScore = (decimal)(random.NextDouble() * 200 - 100) };
            var oi = new V2OiAnalysisResult { TotalOiPillarScore = (decimal)(random.NextDouble() * 200 - 100) };
            var greeks = new V2GreeksAnalysisResult { TotalGreeksPillarScore = (decimal)(random.NextDouble() * 200 - 100) };
            var spot = new V2SpotTrendResult { TotalSpotPillarScore = (decimal)(random.NextDouble() * 200 - 100) };

            var result = _engine.CalculateScoreFromPillars("NIFTY50", 24500, pcr, oi, greeks, spot, _config);

            // Invariant: Total conviction score MUST NEVER exceed [-100, +100]
            Assert.InRange(result.TotalScore, -100.0m, 100.0m);
        }
    }

    [Theory]
    [InlineData(85.0, StrategyRecommendation.StrongBuyCall, TradeCandidateDecision.BuyAtmCeCandidate)]
    [InlineData(65.0, StrategyRecommendation.StrongBuyCall, TradeCandidateDecision.BuyAtmCeCandidate)]
    [InlineData(30.0, StrategyRecommendation.Neutral, TradeCandidateDecision.NoTrade)]
    [InlineData(-10.0, StrategyRecommendation.RangeboundHold, TradeCandidateDecision.NoTrade)]
    [InlineData(-68.0, StrategyRecommendation.StrongBuyPut, TradeCandidateDecision.BuyAtmPeCandidate)]
    [InlineData(-90.0, StrategyRecommendation.StrongBuyPut, TradeCandidateDecision.BuyAtmPeCandidate)]
    public void Recommendation_Mapping_Matches_Configured_Thresholds(
        decimal totalScore, StrategyRecommendation expectedRec, TradeCandidateDecision expectedDecision)
    {
        // Distribute score evenly across 4 pillars
        decimal perPillar = totalScore / 4.0m;
        var pcr = new V2PcrAnalysisResult { TotalPcrPillarScore = perPillar };
        var oi = new V2OiAnalysisResult { TotalOiPillarScore = perPillar };
        var greeks = new V2GreeksAnalysisResult { TotalGreeksPillarScore = perPillar };
        var spot = new V2SpotTrendResult { TotalSpotPillarScore = perPillar };

        var result = _engine.CalculateScoreFromPillars("NIFTY50", 24500, pcr, oi, greeks, spot, _config);

        Assert.Equal(expectedRec, result.Recommendation);
        Assert.Equal(expectedDecision, result.CandidateDecision);
    }
}
