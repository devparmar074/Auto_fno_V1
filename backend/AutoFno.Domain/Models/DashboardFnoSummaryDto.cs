using AutoFno.Domain.Entities;

namespace AutoFno.Domain.Models;

public class DashboardFnoSummaryDto
{
    public string IndexSymbol { get; set; } = "NIFTY50";
    public decimal SpotPrice { get; set; }
    public decimal PrevSpotPrice { get; set; }
    public decimal SpotDayChange { get; set; }
    public decimal SpotDayChangePercent { get; set; }
    public OptionChainDto OptionChain { get; set; } = new();
    public PcrSummaryDto PcrSummary { get; set; } = new();
    public MaxPainDto MaxPain { get; set; } = new();
    public FnOStrategyScoreDto StrategyScore { get; set; } = new();
    public List<PositionDto> OpenPositions { get; set; } = new();
    public decimal TotalUnrealizedPnl { get; set; }
    public decimal TodayRealizedPnl { get; set; }
    public BotConfig BotConfig { get; set; } = new();
    public string MarketStatus { get; set; } = "CLOSED";
    public bool IsTokenValid { get; set; }
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}
