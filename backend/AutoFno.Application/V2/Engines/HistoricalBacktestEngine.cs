using System;
using System.Collections.Generic;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.V2.Engines;

public class HistoricalBacktestEngine : IHistoricalBacktestEngine
{
    private readonly IConvictionScoringEngineV2 _scoringEngine;
    private readonly ITradeQualificationEngine _qualificationEngine;
    private readonly IRiskManagementEngineV2 _riskEngine;
    private readonly ITradeManagementEngineV2 _tradeManagementEngine;
    private readonly ILogger<HistoricalBacktestEngine> _logger;

    public HistoricalBacktestEngine(
        IConvictionScoringEngineV2 scoringEngine,
        ITradeQualificationEngine qualificationEngine,
        IRiskManagementEngineV2 riskEngine,
        ITradeManagementEngineV2 tradeManagementEngine,
        ILogger<HistoricalBacktestEngine> logger)
    {
        _scoringEngine = scoringEngine;
        _qualificationEngine = qualificationEngine;
        _riskEngine = riskEngine;
        _tradeManagementEngine = tradeManagementEngine;
        _logger = logger;
    }

    public HistoricalDataQualityReport InspectDataQuality(List<HistoricalMarketSnapshot> snapshots)
    {
        var report = new HistoricalDataQualityReport
        {
            TotalSnapshotsInspected = snapshots?.Count ?? 0
        };

        if (snapshots == null || snapshots.Count == 0)
        {
            report.IsDataSufficientForBacktesting = false;
            report.SummaryText = "No historical snapshots provided.";
            report.QualityIssuesDetected.Add("Snapshot dataset is completely empty.");
            return report;
        }

        report.Symbol = snapshots[0].Symbol;
        report.EarliestTimestampUtc = snapshots.Min(s => s.TimestampUtc);
        report.LatestTimestampUtc = snapshots.Max(s => s.TimestampUtc);

        DateTime prevTime = DateTime.MinValue;
        int validCount = 0;

        foreach (var snap in snapshots)
        {
            bool hasIssues = false;

            // 1. Timestamp continuity
            if (prevTime != DateTime.MinValue && snap.TimestampUtc <= prevTime)
            {
                report.StaleTimestampCount++;
                hasIssues = true;
            }
            prevTime = snap.TimestampUtc;

            // 2. Option chain completeness
            if (snap.OptionChain == null || snap.OptionChain.Strikes == null || snap.OptionChain.Strikes.Count < 5)
            {
                report.IncompleteChainCount++;
                hasIssues = true;
            }
            else
            {
                // 3. Bid-Ask inversion and zero volume checks
                foreach (var st in snap.OptionChain.Strikes)
                {
                    if (st.Call != null)
                    {
                        if (st.Call.BidPrice > 0 && st.Call.AskPrice > 0 && st.Call.BidPrice > st.Call.AskPrice)
                        {
                            report.InvertedBidAskCount++;
                            hasIssues = true;
                        }
                        if (st.Call.Volume == 0) report.ZeroVolumeCount++;
                    }
                    if (st.Put != null)
                    {
                        if (st.Put.BidPrice > 0 && st.Put.AskPrice > 0 && st.Put.BidPrice > st.Put.AskPrice)
                        {
                            report.InvertedBidAskCount++;
                            hasIssues = true;
                        }
                        if (st.Put.Volume == 0) report.ZeroVolumeCount++;
                    }
                }
            }

            if (!hasIssues) validCount++;
        }

        report.ValidSnapshotsCount = validCount;
        report.DataCompletenessPercent = Math.Round((double)validCount / snapshots.Count * 100.0, 2);
        report.IsDataSufficientForBacktesting = report.DataCompletenessPercent >= 80.0 && report.ValidSnapshotsCount >= 30;

        if (report.IncompleteChainCount > 0)
            report.QualityIssuesDetected.Add($"{report.IncompleteChainCount} snapshots had fewer than 5 option chain strikes.");
        if (report.StaleTimestampCount > 0)
            report.QualityIssuesDetected.Add($"{report.StaleTimestampCount} non-chronological or duplicate timestamps identified.");
        if (report.InvertedBidAskCount > 0)
            report.QualityIssuesDetected.Add($"{report.InvertedBidAskCount} option strikes displayed inverted bid-ask prices (Bid > Ask).");

        report.SummaryText = report.IsDataSufficientForBacktesting
            ? $"Historical data quality verified: {report.ValidSnapshotsCount}/{snapshots.Count} ({report.DataCompletenessPercent}%) valid chronological snapshots."
            : $"Historical data quality insufficient: only {report.DataCompletenessPercent}% valid records (threshold: 80%).";

        return report;
    }

    public HistoricalBacktestResult RunBacktest(List<HistoricalMarketSnapshot> snapshots, HistoricalBacktestRequest request)
    {
        var quality = InspectDataQuality(snapshots);
        var result = new HistoricalBacktestResult
        {
            Symbol = request.Symbol,
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            InitialCapital = request.InitialCapital,
            DataQualityReport = quality,
            TotalSnapshotsEvaluated = snapshots?.Count ?? 0
        };

        if (snapshots == null || snapshots.Count == 0)
        {
            return result;
        }

        // Configuration with friction model
        var config = new QuantitativeConvictionV2Config
        {
            AccountCapital = request.InitialCapital,
            RiskPerTradePercent = request.RiskPerTradePercent,
            BullishCandidateThreshold = request.MinConvictionThreshold,
            BearishCandidateThreshold = -request.MinConvictionThreshold,
            EstimatedSlippagePercent = request.CustomSlippagePercent,
            FrictionModel = request.FrictionModel ?? new FrictionModelConfig()
        };

        // Chronological sort to guarantee no lookahead bias
        var orderedSnapshots = snapshots.OrderBy(s => s.TimestampUtc).ToList();

        decimal currentCapital = request.InitialCapital;
        decimal peakCapital = currentCapital;
        decimal maxDrawdownRupees = 0;
        int currentConsecutiveLosses = 0;
        int maxConsecutiveLosses = 0;
        decimal todayRealizedPnl = 0;
        DateTime currentTradingDay = DateTime.MinValue;

        var openPositions = new List<V2HypotheticalPosition>();
        var closedTrades = new List<V2HypotheticalTradeRecord>();

        foreach (var snap in orderedSnapshots)
        {
            var now = snap.TimestampUtc;

            // Reset daily tracking if new session
            if (snap.TimestampUtc.Date != currentTradingDay)
            {
                currentTradingDay = snap.TimestampUtc.Date;
                todayRealizedPnl = 0;
            }

            // 1. Manage existing open positions
            for (int i = openPositions.Count - 1; i >= 0; i--)
            {
                var pos = openPositions[i];
                if (!pos.IsOpen) continue;

                // Find current option price from snapshot chain
                decimal currentOptionPrice = pos.CurrentPrice;
                var strikeObj = snap.OptionChain?.Strikes?.FirstOrDefault(s => s.StrikePrice == pos.TargetStrike);

                if (strikeObj != null)
                {
                    var contract = pos.OptionType == OptionType.CE ? strikeObj.Call : strikeObj.Put;
                    if (contract != null && contract.Ltp > 0)
                    {
                        // Realistic Execution Model for Exits:
                        if (request.SlippageModel == BacktestExecutionSlippageModel.ConservativeBidAsk && contract.BidPrice > 0)
                        {
                            currentOptionPrice = contract.BidPrice; // Sells at Bid
                        }
                        else
                        {
                            decimal slip = contract.Ltp * (config.EstimatedSlippagePercent / 100m);
                            currentOptionPrice = Math.Max(0.05m, contract.Ltp - slip);
                        }
                    }
                }

                _tradeManagementEngine.UpdatePosition(pos, snap.SpotPrice, currentOptionPrice, now, config, out var exitReason);

                // End-of-day cutoff check (15:15 IST)
                var istTime = now.AddHours(5).AddMinutes(30);
                if (exitReason == V2ExitReason.None && istTime.TimeOfDay >= new TimeSpan(15, 15, 0))
                {
                    exitReason = V2ExitReason.TimeCutoffExit;
                }

                if (exitReason != V2ExitReason.None)
                {
                    var closedRecord = _tradeManagementEngine.ClosePosition(pos, currentOptionPrice, exitReason, now, config);
                    closedTrades.Add(closedRecord);
                    openPositions.RemoveAt(i);

                    currentCapital += closedRecord.NetPnl;
                    todayRealizedPnl += closedRecord.NetPnl;

                    if (closedRecord.NetPnl > 0)
                    {
                        currentConsecutiveLosses = 0;
                    }
                    else
                    {
                        currentConsecutiveLosses++;
                        if (currentConsecutiveLosses > maxConsecutiveLosses)
                            maxConsecutiveLosses = currentConsecutiveLosses;
                    }

                    if (currentCapital > peakCapital)
                    {
                        peakCapital = currentCapital;
                    }
                    else
                    {
                        decimal dd = peakCapital - currentCapital;
                        if (dd > maxDrawdownRupees) maxDrawdownRupees = dd;
                    }
                }
            }

            // 2. Evaluate new candidate signals
            if (openPositions.Count < config.MaxSimultaneousPositions && snap.OptionChain != null)
            {
                var candles = snap.SpotHistory ?? new List<Candle>();
                if (candles.Count == 0 && snap.SpotCandle != null) candles.Add(snap.SpotCandle);

                var score = _scoringEngine.CalculateConvictionScore(
                    snap.OptionChain, snap.PcrSummary, snap.MaxPain, candles, config);

                result.TotalSignalsGenerated++;

                var candidate = _qualificationEngine.QualifyTrade(
                    score, snap.OptionChain, candles, config, openPositions.Count, todayRealizedPnl, currentConsecutiveLosses);

                if (candidate.IsQualified && candidate.AllocatedLots > 0)
                {
                    // Realistic Entry Fill Price:
                    decimal fillEntryPrice = candidate.ExpectedEntryPrice;
                    var targetStrikeObj = snap.OptionChain.Strikes?.FirstOrDefault(s => s.StrikePrice == candidate.TargetStrike);
                    if (targetStrikeObj != null)
                    {
                        var contract = candidate.OptionType == OptionType.CE ? targetStrikeObj.Call : targetStrikeObj.Put;
                        if (contract != null && contract.Ltp > 0)
                        {
                            if (request.SlippageModel == BacktestExecutionSlippageModel.ConservativeBidAsk && contract.AskPrice > 0)
                            {
                                fillEntryPrice = contract.AskPrice; // Buys at Ask
                            }
                            else
                            {
                                decimal slip = contract.Ltp * (config.EstimatedSlippagePercent / 100m);
                                fillEntryPrice = contract.Ltp + slip;
                            }
                        }
                    }

                    var newPos = new V2HypotheticalPosition
                    {
                        IndexSymbol = candidate.IndexSymbol,
                        InstrumentKey = candidate.InstrumentKey,
                        TradingSymbol = candidate.TradingSymbol,
                        TargetStrike = candidate.TargetStrike,
                        OptionType = candidate.OptionType,
                        Lots = candidate.AllocatedLots,
                        Quantity = candidate.AllocatedQuantity,
                        RemainingQuantity = candidate.AllocatedQuantity,
                        EntryPrice = fillEntryPrice,
                        CurrentPrice = fillEntryPrice,
                        PeakPrice = fillEntryPrice,
                        TroughPrice = fillEntryPrice,
                        StopLossPrice = candidate.StopLossPrice,
                        Target1Price = candidate.Target1Price,
                        Target2Price = candidate.Target2Price,
                        InitialRiskPerLot = (fillEntryPrice - candidate.StopLossPrice) + candidate.EstimatedCostPerLot,
                        EntryTimeUtc = now,
                        LastUpdatedUtc = now,
                        IsOpen = true
                    };

                    openPositions.Add(newPos);
                }
            }
        }

        // Close any remaining open positions at last snapshot prices
        if (openPositions.Count > 0 && orderedSnapshots.Count > 0)
        {
            var lastSnap = orderedSnapshots.Last();
            foreach (var pos in openPositions)
            {
                var closed = _tradeManagementEngine.ClosePosition(pos, pos.CurrentPrice, V2ExitReason.TimeCutoffExit, lastSnap.TimestampUtc, config);
                closedTrades.Add(closed);
                currentCapital += closed.NetPnl;
            }
            openPositions.Clear();
        }

        // Compile comprehensive backtest statistics
        CompileBacktestStatistics(result, closedTrades, request.InitialCapital, currentCapital, maxDrawdownRupees, maxConsecutiveLosses);
        return result;
    }

    private void CompileBacktestStatistics(
        HistoricalBacktestResult result,
        List<V2HypotheticalTradeRecord> trades,
        decimal initialCapital,
        decimal endingCapital,
        decimal maxDrawdownRupees,
        int maxConsecutiveLosses)
    {
        result.ExecutedTrades = trades;
        result.TotalTradesExecuted = trades.Count;
        result.EndingCapital = endingCapital;
        result.NetPnl = endingCapital - initialCapital;
        result.ReturnOnCapitalPercent = initialCapital > 0 ? Math.Round((result.NetPnl / initialCapital) * 100m, 2) : 0m;
        result.MaxDrawdownRupees = maxDrawdownRupees;
        result.MaxDrawdownPercent = initialCapital > 0 ? Math.Round((maxDrawdownRupees / initialCapital) * 100m, 2) : 0m;
        result.MaxConsecutiveLosses = maxConsecutiveLosses;

        if (trades.Count == 0) return;

        var winners = trades.Where(t => t.NetPnl > 0).ToList();
        var losers = trades.Where(t => t.NetPnl <= 0).ToList();

        result.WinningTrades = winners.Count;
        result.LosingTrades = losers.Count;
        result.WinRatePercent = Math.Round((decimal)winners.Count / trades.Count * 100m, 2);

        result.GrossProfit = winners.Sum(w => w.GrossPnl);
        result.GrossLoss = Math.Abs(losers.Sum(l => l.GrossPnl));
        result.TotalTransactionCosts = trades.Sum(t => t.EstimatedCosts);

        result.AverageWinner = winners.Count > 0 ? Math.Round(winners.Average(w => w.NetPnl), 2) : 0m;
        result.AverageLoser = losers.Count > 0 ? Math.Round(Math.Abs(losers.Average(l => l.NetPnl)), 2) : 0m;

        result.ProfitFactor = result.GrossLoss > 0 ? Math.Round(result.GrossProfit / result.GrossLoss, 2) : (result.GrossProfit > 0 ? 99.9m : 0m);

        decimal winRate = (decimal)winners.Count / trades.Count;
        decimal lossRate = 1.0m - winRate;
        result.ExpectancyPerTrade = Math.Round((winRate * result.AverageWinner) - (lossRate * result.AverageLoser), 2);

        result.AverageHoldingMinutes = Math.Round(trades.Average(t => t.HoldingMinutes), 1);
        result.AverageMaePercent = Math.Round(trades.Average(t => t.MaePercent), 2);
        result.AverageMfePercent = Math.Round(trades.Average(t => t.MfePercent), 2);

        // Segmentations: CE vs PE
        var ceTrades = trades.Where(t => t.OptionType == OptionType.CE).ToList();
        var peTrades = trades.Where(t => t.OptionType == OptionType.PE).ToList();

        result.CePerformance = CompileSegment("CE Options", ceTrades);
        result.PePerformance = CompileSegment("PE Options", peTrades);

        // Score Bucket Attribution
        result.ScoreBuckets = StratifyScoreBuckets(trades);
    }

    private SegmentPerformance CompileSegment(string name, List<V2HypotheticalTradeRecord> segTrades)
    {
        var seg = new SegmentPerformance { SegmentName = name, TradeCount = segTrades.Count };
        if (segTrades.Count == 0) return seg;

        seg.WinCount = segTrades.Count(t => t.NetPnl > 0);
        seg.WinRatePercent = Math.Round((decimal)seg.WinCount / segTrades.Count * 100m, 2);
        seg.NetPnl = segTrades.Sum(t => t.NetPnl);
        decimal gp = segTrades.Where(t => t.GrossPnl > 0).Sum(t => t.GrossPnl);
        decimal gl = Math.Abs(segTrades.Where(t => t.GrossPnl < 0).Sum(t => t.GrossPnl));
        seg.ProfitFactor = gl > 0 ? Math.Round(gp / gl, 2) : (gp > 0 ? 99.9m : 0m);
        return seg;
    }

    private List<ScoreBucketMetric> StratifyScoreBuckets(List<V2HypotheticalTradeRecord> trades)
    {
        var buckets = new List<ScoreBucketMetric>
        {
            BuildBucket("Bullish +60 to +69", trades.Where(t => t.EntryConvictionScore >= 60 && t.EntryConvictionScore < 70).ToList()),
            BuildBucket("Bullish +70 to +79", trades.Where(t => t.EntryConvictionScore >= 70 && t.EntryConvictionScore < 80).ToList()),
            BuildBucket("Bullish +80 to +89", trades.Where(t => t.EntryConvictionScore >= 80 && t.EntryConvictionScore < 90).ToList()),
            BuildBucket("Bullish +90 to +100", trades.Where(t => t.EntryConvictionScore >= 90).ToList()),
            BuildBucket("Bearish -60 to -69", trades.Where(t => t.EntryConvictionScore <= -60 && t.EntryConvictionScore > -70).ToList()),
            BuildBucket("Bearish -70 to -79", trades.Where(t => t.EntryConvictionScore <= -70 && t.EntryConvictionScore > -80).ToList()),
            BuildBucket("Bearish -80 to -89", trades.Where(t => t.EntryConvictionScore <= -80 && t.EntryConvictionScore > -90).ToList()),
            BuildBucket("Bearish -90 to -100", trades.Where(t => t.EntryConvictionScore <= -90).ToList())
        };
        return buckets;
    }

    private ScoreBucketMetric BuildBucket(string name, List<V2HypotheticalTradeRecord> bTrades)
    {
        var m = new ScoreBucketMetric { BucketRange = name, TradesCount = bTrades.Count };
        if (bTrades.Count == 0) return m;

        m.WinningTrades = bTrades.Count(t => t.NetPnl > 0);
        m.WinRatePercent = Math.Round((decimal)m.WinningTrades / bTrades.Count * 100m, 2);
        m.NetPnl = bTrades.Sum(t => t.NetPnl);
        m.NetExpectancyRupees = Math.Round(m.NetPnl / bTrades.Count, 2);

        decimal gp = bTrades.Where(t => t.GrossPnl > 0).Sum(t => t.GrossPnl);
        decimal gl = Math.Abs(bTrades.Where(t => t.GrossPnl < 0).Sum(t => t.GrossPnl));
        m.ProfitFactor = gl > 0 ? Math.Round(gp / gl, 2) : (gp > 0 ? 99.9m : 0m);
        return m;
    }
}
