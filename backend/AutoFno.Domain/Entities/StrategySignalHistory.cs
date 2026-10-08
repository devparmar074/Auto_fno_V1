using AutoFno.Domain.Enums;

namespace AutoFno.Domain.Entities;

public class StrategySignalHistory
{
    public long Id { get; set; }
    public string IndexSymbol { get; set; } = "NIFTY50";
    public decimal SpotPrice { get; set; }
    public decimal TotalScore { get; set; }
    public decimal PcrScore { get; set; }
    public decimal OiMaxPainScore { get; set; }
    public decimal GreeksIvScore { get; set; }
    public decimal SpotTrendScore { get; set; }
    public StrategyRecommendation Recommendation { get; set; }
    public string? RecommendedStrike { get; set; }
    public OptionType? RecommendedOptionType { get; set; }
    public string? SetupRationale { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
