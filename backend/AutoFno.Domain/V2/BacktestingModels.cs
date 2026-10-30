using System;
using System.Collections.Generic;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;

namespace AutoFno.Domain.V2;

public class HistoricalMarketSnapshot
{
    public DateTime TimestampUtc { get; set; }
    public string Symbol { get; set; } = "NIFTY50";
    public decimal SpotPrice { get; set; }
    public Candle SpotCandle { get; set; } = new();
    public List<Candle> SpotHistory { get; set; } = new();
    public OptionChainDto OptionChain { get; set; } = new();
    public PcrSummaryDto PcrSummary { get; set; } = new();
    public MaxPainDto MaxPain { get; set; } = new();
    public bool IsExpiryDay { get; set; }
}

public class HistoricalBacktestRequest
{
    public string Symbol { get; set; } = "NIFTY50";
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public decimal InitialCapital { get; set; } = 100000m;
    public decimal RiskPerTradePercent { get; set; } = 1.5m;
    public decimal MinConvictionThreshold { get; set; } = 60.0m;
    public BacktestExecutionSlippageModel SlippageModel { get; set; } = BacktestExecutionSlippageModel.ConservativeBidAsk;
    public decimal CustomSlippagePercent { get; set; } = 0.25m;
    public FrictionModelConfig FrictionModel { get; set; } = new();
}

public class HistoricalDataQualityReport
{
    public string Symbol { get; set; } = string.Empty;
    public int TotalSnapshotsInspected { get; set; }
    public int ValidSnapshotsCount { get; set; }
    public int IncompleteChainCount { get; set; }
    public int StaleTimestampCount { get; set; }
    public int InvertedBidAskCount { get; set; }
    public int ZeroVolumeCount { get; set; }
    public DateTime EarliestTimestampUtc { get; set; }
    public DateTime LatestTimestampUtc { get; set; }
    public double DataCompletenessPercent { get; set; }
    public bool IsDataSufficientForBacktesting { get; set; }
    public List<string> QualityIssuesDetected { get; set; } = new();
    public string SummaryText { get; set; } = string.Empty;
}

public class HistoricalBacktestResult
{
    public string Symbol { get; set; } = "NIFTY50";
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int TotalSnapshotsEvaluated { get; set; }
    public int TotalSignalsGenerated { get; set; }
    public int TotalTradesExecuted { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRatePercent { get; set; }

    public decimal InitialCapital { get; set; }
    public decimal EndingCapital { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossLoss { get; set; }
    public decimal NetPnl { get; set; }
    public decimal TotalTransactionCosts { get; set; }
    public decimal ReturnOnCapitalPercent { get; set; }

    public decimal AverageWinner { get; set; }
    public decimal AverageLoser { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal ExpectancyPerTrade { get; set; }
    public decimal MaxDrawdownRupees { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public double AverageHoldingMinutes { get; set; }
    public decimal AverageMaePercent { get; set; }
    public decimal AverageMfePercent { get; set; }

    // Segmentations
    public SegmentPerformance CePerformance { get; set; } = new();
    public SegmentPerformance PePerformance { get; set; } = new();
    public SegmentPerformance ExpiryDayPerformance { get; set; } = new();
    public SegmentPerformance RegularDayPerformance { get; set; } = new();

    public List<ScoreBucketMetric> ScoreBuckets { get; set; } = new();
    public List<V2HypotheticalTradeRecord> ExecutedTrades { get; set; } = new();
    public HistoricalDataQualityReport DataQualityReport { get; set; } = new();
}

public class SegmentPerformance
{
    public string SegmentName { get; set; } = string.Empty;
    public int TradeCount { get; set; }
    public int WinCount { get; set; }
    public decimal WinRatePercent { get; set; }
    public decimal NetPnl { get; set; }
    public decimal ProfitFactor { get; set; }
}
