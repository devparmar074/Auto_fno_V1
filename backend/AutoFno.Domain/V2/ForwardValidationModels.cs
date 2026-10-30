using System;
using System.Collections.Generic;

namespace AutoFno.Domain.V2;

public class ForwardValidationCampaignConfig
{
    public Guid CampaignId { get; set; } = Guid.NewGuid();
    public string BaselineVersion { get; set; } = "v2.0-baseline";
    public string Symbol { get; set; } = "NIFTY50";
    public DateTime StartDateUtc { get; set; } = DateTime.UtcNow;
    public int RequiredCalendarDays { get; set; } = 30;
    public int MinimumTradeSamples { get; set; } = 50;

    // Acceptance Gates
    public decimal MinimumNetExpectancy { get; set; } = 200m;
    public decimal MinimumProfitFactor { get; set; } = 1.5m;
    public decimal MinimumWinRatePercent { get; set; } = 55.0m;
    public decimal MaximumPermissibleDrawdownPercent { get; set; } = 6.0m;
    public decimal MinimumScoreMonotonicityScore { get; set; } = 0.70m; // Spearman correlation between score bucket & winrate
}

public class ValidationRegimeMetric
{
    public MarketRegime Regime { get; set; }
    public int SignalCount { get; set; }
    public int TradeCount { get; set; }
    public int WinCount { get; set; }
    public decimal WinRatePercent { get; set; }
    public decimal NetPnl { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal NetExpectancy { get; set; }
}

public class ValidationWeeklyReport
{
    public int WeekNumber { get; set; }
    public DateTime WeekStartDateUtc { get; set; }
    public DateTime WeekEndDateUtc { get; set; }
    public int MarketSessionsObserved { get; set; }
    public int SignalsObserved { get; set; }
    public int TradesExecuted { get; set; }
    public decimal WinRatePercent { get; set; }
    public decimal NetPnl { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public string MarketSummary { get; set; } = string.Empty;
}

public class ForwardValidationStatusDto
{
    public Guid CampaignId { get; set; }
    public string BaselineVersion { get; set; } = "v2.0-baseline";
    public ValidationCampaignStatus Status { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime TargetCompletionDateUtc { get; set; }
    public int ElapsedCalendarDays { get; set; }
    public int ElapsedMarketSessions { get; set; }

    public int TotalSignalsObserved { get; set; }
    public int TotalRejectedSignals { get; set; }
    public int TotalTradesRecorded { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }

    public decimal CurrentWinRatePercent { get; set; }
    public decimal CurrentNetPnl { get; set; }
    public decimal CurrentNetExpectancy { get; set; }
    public decimal CurrentProfitFactor { get; set; }
    public decimal CurrentMaxDrawdownPercent { get; set; }

    public decimal MonotonicityScore { get; set; } // Bucket consistency
    public bool HasMetSampleCount { get; set; }
    public bool HasElapsedFullDuration { get; set; }
    public bool HasPassedAllAcceptanceGates { get; set; }

    public List<ValidationRegimeMetric> RegimeBreakdowns { get; set; } = new();
    public List<ScoreBucketMetric> ScoreBuckets { get; set; } = new();
    public List<ValidationWeeklyReport> WeeklyReports { get; set; } = new();
    public string FinalDecisionSummary { get; set; } = string.Empty;
}
