using AutoFno.Domain.Enums;

namespace AutoFno.Domain.Models;

public class FnOStrategyScoreDto
{
    public string IndexSymbol { get; set; } = "NIFTY50";
    public decimal SpotPrice { get; set; }
    public decimal TotalScore { get; set; } // -100 to +100
    public decimal PcrScore { get; set; } // Max 25
    public decimal OiMaxPainScore { get; set; } // Max 25
    public decimal GreeksIvScore { get; set; } // Max 25
    public decimal SpotTrendScore { get; set; } // Max 25
    public StrategyRecommendation Recommendation { get; set; } = StrategyRecommendation.Neutral;
    public string SetupRationale { get; set; } = string.Empty;
    public string RecommendedStrike { get; set; } = string.Empty;
    public OptionType? RecommendedOptionType { get; set; }
    public TransactionType RecommendedAction { get; set; } = TransactionType.BUY;
    public decimal RecommendedEntryPrice { get; set; }
    public decimal RecommendedStopLoss { get; set; }
    public decimal RecommendedTarget { get; set; }
    public decimal ExpectedDelta { get; set; }
    public decimal ExpectedThetaDecay { get; set; }
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
}
