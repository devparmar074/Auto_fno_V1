using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoFno.Tests.V2;

public class SelfOptimizingMLTests
{
    private readonly SelfOptimizingEngine _engine;

    public SelfOptimizingMLTests()
    {
        _engine = new SelfOptimizingEngine(new NullLogger<SelfOptimizingEngine>());
    }

    [Fact]
    public void ExtractFeatures_ShouldPopulateAllFourPillarDimensions()
    {
        var chain = new OptionChainDto { UnderlyingSymbol = "NIFTY50", SpotPrice = 24500m };
        var score = new V2ConvictionScoreDto
        {
            TotalScore = 68.0m,
            PcrAnalysis = new V2PcrAnalysisResult { OiPcr = 1.35m, PcrSlope = 0.08m },
            OiAnalysis = new V2OiAnalysisResult { NetBuildup = 4, DistanceToMaxPain = 50m },
            GreeksAnalysis = new V2GreeksAnalysisResult { AtmDeltaCe = 0.52m, AtmIv = 14.5m, IvSkew = -1.2m },
            SpotTrend = new V2SpotTrendResult { Rsi14 = 62.0m, IsSupertrendBullish = true }
        };

        var vec = _engine.ExtractFeatures(chain, score, new List<Candle>(), MarketRegime.StrongBullTrend);

        Assert.Equal("NIFTY50", vec.Symbol);
        Assert.Equal(68.0m, vec.TotalConvictionScore);
        Assert.Equal(1.35m, vec.OiPcr);
        Assert.Equal(4, vec.NetBuildup);
        Assert.Equal(0.52m, vec.AtmDelta);
        Assert.Equal(62.0m, vec.Rsi14);
        Assert.Equal(1, vec.SupertrendDirection);
    }

    [Fact]
    public async Task RunOptimization_WithSufficientDataset_ShouldGenerateCandidateWithWalkForwardMetrics()
    {
        var dataset = new List<MLFeatureVector>();
        for (int i = 0; i < 30; i++)
        {
            dataset.Add(new MLFeatureVector
            {
                TimestampUtc = DateTime.UtcNow.AddMinutes(i),
                OiPcr = 1.25m,
                NetBuildup = 3,
                AtmIv = 14.0m,
                SupertrendDirection = 1,
                TargetForwardOptionReturnPercent = i % 3 == 0 ? -15m : 25m,
                TargetBinaryWinLoss = i % 3 == 0 ? 0 : 1
            });
        }

        var result = await _engine.RunOptimizationAsync(dataset, OptimizationObjective.Expectancy);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Metadata);
        Assert.Equal(MLModelStatus.Candidate, result.Metadata.Status);
        Assert.NotEmpty(result.WalkForwardWindows);
        Assert.True(result.CandidateExpectancy > 0);
    }

    [Fact]
    public void CheckForDrift_WhenObservationsStable_ShouldReportNoCriticalDrift()
    {
        var observations = new List<MLFeatureVector>();
        for (int i = 0; i < 15; i++)
        {
            observations.Add(new MLFeatureVector
            {
                TotalConvictionScore = 5m,
                TargetBinaryWinLoss = 1
            });
        }

        var drift = _engine.CheckForDrift(observations);

        Assert.NotNull(drift);
        Assert.False(drift.ShouldTriggerRollback);
    }

    [Fact]
    public async Task RollbackToBaseline_AfterPromotion_ShouldRestoreDefaultWeights()
    {
        // 1. Generate candidate model
        var dataset = new List<MLFeatureVector>();
        for (int i = 0; i < 25; i++)
        {
            dataset.Add(new MLFeatureVector { OiPcr = 1.3m, NetBuildup = 2, TargetForwardOptionReturnPercent = 20m });
        }
        var trainResult = await _engine.RunOptimizationAsync(dataset, OptimizationObjective.Expectancy);
        Assert.True(trainResult.IsSuccess);

        // 2. Promote candidate
        bool promoted = _engine.PromoteCandidateModel(trainResult.Metadata.ModelId);
        Assert.True(promoted);
        Assert.Equal(MLModelStatus.Active, _engine.GetActiveModelMetadata().Status);

        // 3. Rollback to baseline
        bool rolledBack = _engine.RollbackToBaseline();
        Assert.True(rolledBack);
        var active = _engine.GetActiveModelMetadata();
        Assert.Equal("v2.0-baseline-weights", active.Version);
        Assert.Equal(1.0m, active.WeightPcr);
        Assert.Equal(1.0m, active.WeightOi);
        Assert.Equal(1.0m, active.WeightGreeks);
        Assert.Equal(1.0m, active.WeightSpotTrend);
    }
}
