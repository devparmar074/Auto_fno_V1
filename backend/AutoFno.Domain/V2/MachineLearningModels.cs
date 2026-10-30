using System;
using System.Collections.Generic;

namespace AutoFno.Domain.V2;

public class MLFeatureVector
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Symbol { get; set; } = "NIFTY50";

    // Pillar 1: PCR
    public decimal OiPcr { get; set; }
    public decimal VolumePcr { get; set; }
    public decimal AtmPcr { get; set; }
    public decimal PcrSlope { get; set; }

    // Pillar 2: OI Distribution
    public decimal NetBuildup { get; set; }
    public decimal MaxPainDistance { get; set; }
    public decimal CallWallDistance { get; set; }
    public decimal PutWallDistance { get; set; }

    // Pillar 3: Greeks & IV
    public decimal AtmDelta { get; set; }
    public decimal AtmGamma { get; set; }
    public decimal AtmVega { get; set; }
    public decimal AtmTheta { get; set; }
    public decimal AtmIv { get; set; }
    public decimal IvSkew { get; set; }

    // Pillar 4: Spot Momentum
    public decimal EmaSeparationAtrNormalized { get; set; }
    public decimal Rsi14 { get; set; }
    public decimal VwapDistance { get; set; }
    public int SupertrendDirection { get; set; } // +1 or -1
    public int MarketRegimeValue { get; set; }

    // Baseline Scores
    public decimal TotalConvictionScore { get; set; }

    // Targets for Training
    public decimal TargetForwardOptionReturnPercent { get; set; }
    public int TargetBinaryWinLoss { get; set; } // 1 = Win, 0 = Loss
    public decimal RealizedMaePercent { get; set; }
    public decimal RealizedMfePercent { get; set; }
}

public class MLModelMetadata
{
    public Guid ModelId { get; set; } = Guid.NewGuid();
    public string Version { get; set; } = "v2.0-ml-init";
    public MLModelStatus Status { get; set; } = MLModelStatus.Untrained;
    public OptimizationObjective Objective { get; set; } = OptimizationObjective.Expectancy;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? RolledBackAtUtc { get; set; }

    public int TotalTrainingSamples { get; set; }
    public int TotalValidationSamples { get; set; }

    public decimal InSampleProfitFactor { get; set; }
    public decimal OutOfSampleProfitFactor { get; set; }
    public decimal InSampleExpectancy { get; set; }
    public decimal OutOfSampleExpectancy { get; set; }
    public decimal WalkForwardEfficiencyRatio { get; set; } // OOS / IS ratio

    // Dynamic Pillar Weights
    public decimal WeightPcr { get; set; } = 1.0m;
    public decimal WeightOi { get; set; } = 1.0m;
    public decimal WeightGreeks { get; set; } = 1.0m;
    public decimal WeightSpotTrend { get; set; } = 1.0m;

    // Adaptive Thresholds
    public decimal MinConvictionThreshold { get; set; } = 60.0m;
    public decimal IvCrushThreshold { get; set; } = 85.0m;

    public Dictionary<string, decimal> FeatureImportances { get; set; } = new();
    public string GovernanceNotes { get; set; } = string.Empty;
}

public class MLWalkForwardWindowResult
{
    public int WindowIndex { get; set; }
    public DateTime TrainStartUtc { get; set; }
    public DateTime TrainEndUtc { get; set; }
    public DateTime TestStartUtc { get; set; }
    public DateTime TestEndUtc { get; set; }

    public int TrainSamplesCount { get; set; }
    public int TestSamplesCount { get; set; }

    public decimal TrainExpectancy { get; set; }
    public decimal TestExpectancy { get; set; }
    public decimal TrainProfitFactor { get; set; }
    public decimal TestProfitFactor { get; set; }
    public decimal TestMaxDrawdownPercent { get; set; }
    public bool IsRobust { get; set; }
}

public class MLTrainingResult
{
    public bool IsSuccess { get; set; }
    public MLModelMetadata Metadata { get; set; } = new();
    public List<MLWalkForwardWindowResult> WalkForwardWindows { get; set; } = new();
    public decimal OverallObjectiveScore { get; set; }
    public decimal BaselineExpectancy { get; set; }
    public decimal CandidateExpectancy { get; set; }
    public decimal NetImprovementPercent { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
}

public class MLDriftReport
{
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ModelVersion { get; set; } = string.Empty;
    public decimal PopulationStabilityIndex { get; set; } // PSI
    public bool IsFeatureDriftDetected { get; set; }
    public bool IsPerformanceDegradationDetected { get; set; }
    public bool ShouldTriggerRollback { get; set; }
    public string SummaryText { get; set; } = string.Empty;
}
