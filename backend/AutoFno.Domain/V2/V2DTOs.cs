using System;
using System.Collections.Generic;
using AutoFno.Domain.Enums;

namespace AutoFno.Domain.V2;

public class V2PcrAnalysisResult
{
    public decimal VolumePcrFlowScore { get; set; } // Max 10 pts
    public decimal AtmPcrScore { get; set; }         // Max 7 pts
    public decimal PcrSlopeVelocityScore { get; set; }// Max 5 pts
    public decimal BroaderOiPcrScore { get; set; }   // Max 3 pts
    public decimal TotalPcrPillarScore { get; set; } // Max 25 pts (-25 to +25)

    // Raw Metrics
    public decimal OiPcr { get; set; }
    public decimal VolumePcr { get; set; }
    public decimal AtmPcr { get; set; }
    public decimal PcrSlope { get; set; }
    public bool PutWritingEvidence { get; set; }
    public bool CallWritingEvidence { get; set; }
    public string Interpretation { get; set; } = string.Empty;
}

public class V2OiAnalysisResult
{
    public decimal BuildupDominanceScore { get; set; }    // Max 10 pts
    public decimal FreshWritingFlowScore { get; set; }    // Max 8 pts
    public decimal OiSupportResistanceScore { get; set; } // Max 5 pts
    public decimal MaxPainDistanceScore { get; set; }     // Max 2 pts
    public decimal TotalOiPillarScore { get; set; }       // Max 25 pts (-25 to +25)

    // Raw Metrics
    public int LongBuildupCount { get; set; }
    public int ShortBuildupCount { get; set; }
    public int ShortCoveringCount { get; set; }
    public int LongUnwindingCount { get; set; }
    public int NetBuildup { get; set; }
    public decimal CallWallStrike { get; set; }
    public decimal PutWallStrike { get; set; }
    public decimal MaxPainStrike { get; set; }
    public decimal DistanceToMaxPain { get; set; }
    public decimal PutOiAdditionTotal { get; set; }
    public decimal CallOiAdditionTotal { get; set; }
}

public class V2GreeksAnalysisResult
{
    public decimal AtmDeltaAsymmetryScore { get; set; }   // Max 6 pts
    public decimal IvRegimeMomentumScore { get; set; }    // Max 7 pts
    public decimal CallPutIvSkewScore { get; set; }       // Max 5 pts
    public decimal GammaThetaEfficiencyScore { get; set; }// Max 7 pts
    public decimal TotalGreeksPillarScore { get; set; }   // Max 25 pts (-25 to +25)

    // Greeks & Volatility Metrics
    public decimal AtmDeltaCe { get; set; }
    public decimal AtmDeltaPe { get; set; }
    public decimal AtmGamma { get; set; }
    public decimal AtmTheta { get; set; }
    public decimal AtmVega { get; set; }
    public decimal AtmIv { get; set; }
    public decimal IvSkew { get; set; } // Call IV - Put IV
    public IvEnvironment IvEnvironment { get; set; }

    // Option Premium Movement Estimation Model (Taylor Expansion)
    // dP = (Delta * dS) + (0.5 * Gamma * dS^2) + (Vega * dIV) + (Theta * dt)
    public decimal EstimatedPremiumChangeCe { get; set; }
    public decimal EstimatedPremiumChangePe { get; set; }
    public bool IsExpectedPremiumMoveSufficient { get; set; }
}

public class V2SpotTrendResult
{
    public decimal EmaAlignmentSlopeScore { get; set; }   // Max 8 pts
    public decimal SupertrendDirectionScore { get; set; } // Max 5 pts
    public decimal RsiMomentumRegimeScore { get; set; }   // Max 4 pts
    public decimal VwapPriceStructureScore { get; set; }  // Max 8 pts
    public decimal TotalSpotPillarScore { get; set; }     // Max 25 pts (-25 to +25)

    // Technical Metrics
    public decimal SpotPrice { get; set; }
    public decimal Ema9 { get; set; }
    public decimal Ema21 { get; set; }
    public decimal EmaSeparationAtrNormalized { get; set; }
    public decimal Rsi14 { get; set; }
    public decimal Vwap { get; set; }
    public bool IsSupertrendBullish { get; set; }
    public MarketRegime Regime { get; set; }
    public bool IsOverextended { get; set; }
}

public class V2ConvictionScoreDto
{
    public string IndexSymbol { get; set; } = "NIFTY50";
    public decimal SpotPrice { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public V2PcrAnalysisResult PcrAnalysis { get; set; } = new();
    public V2OiAnalysisResult OiAnalysis { get; set; } = new();
    public V2GreeksAnalysisResult GreeksAnalysis { get; set; } = new();
    public V2SpotTrendResult SpotTrend { get; set; } = new();

    public decimal TotalScore { get; set; } // -100 to +100
    public StrategyRecommendation Recommendation { get; set; }
    public TradeCandidateDecision CandidateDecision { get; set; }
    public string SetupRationale { get; set; } = string.Empty;
}

public class V2TradeCandidate
{
    public TradeCandidateDecision Decision { get; set; } = TradeCandidateDecision.NoTrade;
    public bool IsQualified { get; set; }
    public QualificationRejectionReason RejectionReasons { get; set; } = QualificationRejectionReason.None;
    public List<string> RejectionDetails { get; set; } = new();

    public string IndexSymbol { get; set; } = "NIFTY50";
    public decimal SpotPrice { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public decimal TargetStrike { get; set; }
    public OptionType OptionType { get; set; }
    public string InstrumentKey { get; set; } = string.Empty;
    public string TradingSymbol { get; set; } = string.Empty;

    public decimal ExpectedEntryPrice { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal Target1Price { get; set; }
    public decimal Target2Price { get; set; }
    public decimal RiskRewardRatio { get; set; }

    public decimal ExpectedMovePremium { get; set; }
    public decimal EstimatedCostPerLot { get; set; }
    public int AllocatedLots { get; set; }
    public int AllocatedQuantity { get; set; }
    public decimal TotalRiskRupees { get; set; }
    public decimal ConvictionScore { get; set; }
}

public class V2HypotheticalPosition
{
    public Guid PositionId { get; set; } = Guid.NewGuid();
    public string IndexSymbol { get; set; } = string.Empty;
    public string InstrumentKey { get; set; } = string.Empty;
    public string TradingSymbol { get; set; } = string.Empty;
    public OptionType OptionType { get; set; }
    public decimal TargetStrike { get; set; }
    public int Lots { get; set; }
    public int Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal Target1Price { get; set; }
    public decimal Target2Price { get; set; }
    public decimal InitialRiskPerLot { get; set; }

    public bool IsPartiallyExited { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnrealizedPnl { get; set; }
    public decimal RealizedPnl { get; set; }
    public decimal PeakPrice { get; set; }
    public decimal TroughPrice { get; set; }

    public DateTime EntryTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsOpen { get; set; } = true;
    public V2ExitReason ExitReason { get; set; } = V2ExitReason.None;
}

public class V2HypotheticalTradeRecord
{
    public Guid TradeId { get; set; } = Guid.NewGuid();
    public string IndexSymbol { get; set; } = string.Empty;
    public string TradingSymbol { get; set; } = string.Empty;
    public OptionType OptionType { get; set; }
    public int Lots { get; set; }
    public int Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal GrossPnl { get; set; }
    public decimal EstimatedCosts { get; set; }
    public decimal NetPnl { get; set; }
    public decimal RMultipleAchieved { get; set; }
    public double HoldingMinutes { get; set; }
    public decimal MaePercent { get; set; }
    public decimal MfePercent { get; set; }
    public V2ExitReason ExitReason { get; set; }
    public decimal EntryConvictionScore { get; set; }
    public DateTime EntryTimeUtc { get; set; }
    public DateTime ExitTimeUtc { get; set; }
}

public class ScoreBucketMetric
{
    public string BucketRange { get; set; } = string.Empty; // e.g. "+60 to +69"
    public int TotalSignals { get; set; }
    public int TradesCount { get; set; }
    public int WinningTrades { get; set; }
    public decimal WinRatePercent { get; set; }
    public decimal NetPnl { get; set; }
    public decimal NetExpectancyRupees { get; set; }
    public decimal ProfitFactor { get; set; }
}

public class V2PerformanceMetricsDto
{
    public int TotalSignalsEvaluated { get; set; }
    public int QualifiedSignalsCount { get; set; }
    public int RejectedSignalsCount { get; set; }
    public int TotalTradesExecuted { get; set; }
    public int WinningTradesCount { get; set; }
    public int LosingTradesCount { get; set; }

    public decimal WinRatePercent { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossLoss { get; set; }
    public decimal NetPnl { get; set; }
    public decimal TotalEstimatedCosts { get; set; }

    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal NetExpectancyRupees { get; set; }
    public decimal MaxDrawdownRupees { get; set; }
    public int MaxConsecutiveLosses { get; set; }

    public List<ScoreBucketMetric> ScoreBuckets { get; set; } = new();
    public List<V2HypotheticalTradeRecord> RecentTrades { get; set; } = new();
}

public class V1VsV2ComparisonDto
{
    public int V1TotalSignals { get; set; }
    public int V2TotalSignals { get; set; }
    public int V1QualifiedTrades { get; set; }
    public int V2QualifiedTrades { get; set; }
    public decimal V1WinRate { get; set; }
    public decimal V2HypotheticalWinRate { get; set; }
    public decimal V1NetPnl { get; set; }
    public decimal V2HypotheticalNetPnl { get; set; }
    public decimal V1ProfitFactor { get; set; }
    public decimal V2ProfitFactor { get; set; }
    public decimal V1MaxDrawdown { get; set; }
    public decimal V2MaxDrawdown { get; set; }
    public decimal V1AvgRiskPerTrade { get; set; }
    public decimal V2AvgRiskPerTrade { get; set; }
    public decimal V2NetExpectancy { get; set; }
    public string SummaryEvaluation { get; set; } = string.Empty;
}
