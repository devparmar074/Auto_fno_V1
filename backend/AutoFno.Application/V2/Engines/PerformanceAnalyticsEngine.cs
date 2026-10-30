using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class PerformanceAnalyticsEngine : IPerformanceAnalyticsEngine
{
    private readonly object _lock = new();
    private readonly List<V2HypotheticalTradeRecord> _trades = new();
    private int _totalSignals;
    private int _qualifiedSignals;
    private int _rejectedSignals;

    public void RecordSignal(V2ConvictionScoreDto score, V2TradeCandidate candidate)
    {
        lock (_lock)
        {
            _totalSignals++;
            if (candidate.IsQualified) _qualifiedSignals++;
            else _rejectedSignals++;
        }
    }

    public void RecordTrade(V2HypotheticalTradeRecord trade)
    {
        lock (_lock)
        {
            _trades.Add(trade);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _trades.Clear();
            _totalSignals = 0;
            _qualifiedSignals = 0;
            _rejectedSignals = 0;
        }
    }

    public V2PerformanceMetricsDto GetPerformanceMetrics()
    {
        lock (_lock)
        {
            var trades = _trades.ToList();
            var totalTrades = trades.Count;
            var winningTrades = trades.Where(t => t.NetPnl > 0).ToList();
            var losingTrades = trades.Where(t => t.NetPnl <= 0).ToList();

            var grossProfit = winningTrades.Sum(t => t.GrossPnl);
            var grossLoss = Math.Abs(losingTrades.Sum(t => t.GrossPnl));
            var totalCosts = trades.Sum(t => t.EstimatedCosts);
            var netPnl = trades.Sum(t => t.NetPnl);

            var winRate = totalTrades > 0 ? (decimal)winningTrades.Count / totalTrades * 100m : 0m;
            var avgWin = winningTrades.Count > 0 ? winningTrades.Average(t => t.NetPnl) : 0m;
            var avgLoss = losingTrades.Count > 0 ? Math.Abs(losingTrades.Average(t => t.NetPnl)) : 0m;

            // Net Expectancy Formula:
            // Expectancy = (P(Win) * Avg Win) - (P(Loss) * Avg Loss)
            var pWin = totalTrades > 0 ? (decimal)winningTrades.Count / totalTrades : 0m;
            var pLoss = totalTrades > 0 ? (decimal)losingTrades.Count / totalTrades : 0m;
            var expectancy = (pWin * avgWin) - (pLoss * avgLoss);

            // Profit Factor = Gross Profit / Gross Loss
            var profitFactor = grossLoss > 0 ? Math.Round(grossProfit / grossLoss, 2) : (grossProfit > 0 ? 99.9m : 0m);

            // Maximum Drawdown & Consecutive Losses
            decimal peakEquity = 0m;
            decimal currentEquity = 0m;
            decimal maxDrawdown = 0m;
            int maxConsecutiveLosses = 0;
            int currentConsecutiveLosses = 0;

            foreach (var t in trades)
            {
                currentEquity += t.NetPnl;
                if (currentEquity > peakEquity) peakEquity = currentEquity;
                var dd = peakEquity - currentEquity;
                if (dd > maxDrawdown) maxDrawdown = dd;

                if (t.NetPnl <= 0)
                {
                    currentConsecutiveLosses++;
                    if (currentConsecutiveLosses > maxConsecutiveLosses) maxConsecutiveLosses = currentConsecutiveLosses;
                }
                else
                {
                    currentConsecutiveLosses = 0;
                }
            }

            // Score Bucketing Analysis
            var buckets = new List<ScoreBucketMetric>();
            var bucketDefs = new[]
            {
                ("+90 to +100", 90m, 100m),
                ("+80 to +89", 80m, 89.9m),
                ("+70 to +79", 70m, 79.9m),
                ("+60 to +69", 60m, 69.9m),
                ("-60 to -69", -69.9m, -60m),
                ("-70 to -79", -79.9m, -70m),
                ("-80 to -89", -89.9m, -80m),
                ("-90 to -100", -100m, -90m)
            };

            foreach (var (label, min, max) in bucketDefs)
            {
                var bucketTrades = trades.Where(t => t.EntryConvictionScore >= min && t.EntryConvictionScore <= max).ToList();
                var bWins = bucketTrades.Where(t => t.NetPnl > 0).ToList();
                var bLosses = bucketTrades.Where(t => t.NetPnl <= 0).ToList();

                var bGrossProf = bWins.Sum(t => t.GrossPnl);
                var bGrossLoss = Math.Abs(bLosses.Sum(t => t.GrossPnl));
                var bPf = bGrossLoss > 0 ? Math.Round(bGrossProf / bGrossLoss, 2) : 0m;

                var bPWin = bucketTrades.Count > 0 ? (decimal)bWins.Count / bucketTrades.Count : 0m;
                var bPLoss = bucketTrades.Count > 0 ? (decimal)bLosses.Count / bucketTrades.Count : 0m;
                var bAvgWin = bWins.Count > 0 ? bWins.Average(t => t.NetPnl) : 0m;
                var bAvgLoss = bLosses.Count > 0 ? Math.Abs(bLosses.Average(t => t.NetPnl)) : 0m;
                var bExp = (bPWin * bAvgWin) - (bPLoss * bAvgLoss);

                buckets.Add(new ScoreBucketMetric
                {
                    BucketRange = label,
                    TradesCount = bucketTrades.Count,
                    WinningTrades = bWins.Count,
                    WinRatePercent = bucketTrades.Count > 0 ? Math.Round((decimal)bWins.Count / bucketTrades.Count * 100m, 1) : 0m,
                    NetPnl = Math.Round(bucketTrades.Sum(t => t.NetPnl), 2),
                    NetExpectancyRupees = Math.Round(bExp, 2),
                    ProfitFactor = bPf
                });
            }

            return new V2PerformanceMetricsDto
            {
                TotalSignalsEvaluated = _totalSignals,
                QualifiedSignalsCount = _qualifiedSignals,
                RejectedSignalsCount = _rejectedSignals,
                TotalTradesExecuted = totalTrades,
                WinningTradesCount = winningTrades.Count,
                LosingTradesCount = losingTrades.Count,
                WinRatePercent = Math.Round(winRate, 2),
                GrossProfit = Math.Round(grossProfit, 2),
                GrossLoss = Math.Round(grossLoss, 2),
                NetPnl = Math.Round(netPnl, 2),
                TotalEstimatedCosts = Math.Round(totalCosts, 2),
                AverageWin = Math.Round(avgWin, 2),
                AverageLoss = Math.Round(avgLoss, 2),
                ProfitFactor = profitFactor,
                NetExpectancyRupees = Math.Round(expectancy, 2),
                MaxDrawdownRupees = Math.Round(maxDrawdown, 2),
                MaxConsecutiveLosses = maxConsecutiveLosses,
                ScoreBuckets = buckets,
                RecentTrades = trades.TakeLast(20).Reverse().ToList()
            };
        }
    }
}
