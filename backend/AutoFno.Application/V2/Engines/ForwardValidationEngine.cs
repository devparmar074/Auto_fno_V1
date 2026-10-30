using System;
using System.Collections.Generic;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.V2.Engines;

public class ForwardValidationEngine : IForwardValidationEngine
{
    private readonly ILogger<ForwardValidationEngine> _logger;
    private ForwardValidationCampaignConfig _config = new();
    private ForwardValidationStatusDto _status = new();
    private readonly List<V2HypotheticalTradeRecord> _campaignTrades = new();
    private readonly List<ValidationWeeklyReport> _weeklyReports = new();
    private readonly Dictionary<MarketRegime, ValidationRegimeMetric> _regimeMetrics = new();
    private readonly HashSet<DateTime> _observedMarketSessions = new();
    private readonly object _lock = new();

    public ForwardValidationEngine(ILogger<ForwardValidationEngine> logger)
    {
        _logger = logger;
        InitializeDefaultCampaign();
    }

    private void InitializeDefaultCampaign()
    {
        _config = new ForwardValidationCampaignConfig
        {
            CampaignId = Guid.NewGuid(),
            BaselineVersion = "v2.0-baseline",
            Symbol = "NIFTY50",
            StartDateUtc = DateTime.UtcNow,
            RequiredCalendarDays = 30,
            MinimumTradeSamples = 50,
            MinimumNetExpectancy = 200m,
            MinimumProfitFactor = 1.5m,
            MinimumWinRatePercent = 55.0m,
            MaximumPermissibleDrawdownPercent = 6.0m
        };

        _status = new ForwardValidationStatusDto
        {
            CampaignId = _config.CampaignId,
            BaselineVersion = _config.BaselineVersion,
            Status = ValidationCampaignStatus.Active,
            StartDateUtc = _config.StartDateUtc,
            TargetCompletionDateUtc = _config.StartDateUtc.AddDays(_config.RequiredCalendarDays),
            FinalDecisionSummary = "Campaign active and observing live shadow paper trading signals."
        };
    }

    public ForwardValidationStatusDto GetCurrentCampaignStatus()
    {
        lock (_lock)
        {
            UpdateMetricsUnsafe();
            return _status;
        }
    }

    public ForwardValidationStatusDto StartCampaign(ForwardValidationCampaignConfig config)
    {
        lock (_lock)
        {
            _config = config ?? new ForwardValidationCampaignConfig();
            _campaignTrades.Clear();
            _weeklyReports.Clear();
            _regimeMetrics.Clear();
            _observedMarketSessions.Clear();

            _status = new ForwardValidationStatusDto
            {
                CampaignId = _config.CampaignId,
                BaselineVersion = _config.BaselineVersion,
                Status = ValidationCampaignStatus.Active,
                StartDateUtc = _config.StartDateUtc,
                TargetCompletionDateUtc = _config.StartDateUtc.AddDays(_config.RequiredCalendarDays),
                FinalDecisionSummary = $"Fresh forward validation campaign '{_config.BaselineVersion}' started for {_config.RequiredCalendarDays} calendar days."
            };

            _logger.LogInformation("Started V2 Forward Validation Campaign {Id} ({Version}).", _config.CampaignId, _config.BaselineVersion);
            return _status;
        }
    }

    public void RecordDailyObservation(DateTime dateUtc, List<V2HypotheticalTradeRecord> dayTrades, MarketRegime predominantRegime)
    {
        lock (_lock)
        {
            _observedMarketSessions.Add(dateUtc.Date);

            if (dayTrades != null && dayTrades.Count > 0)
            {
                _campaignTrades.AddRange(dayTrades);

                if (!_regimeMetrics.ContainsKey(predominantRegime))
                {
                    _regimeMetrics[predominantRegime] = new ValidationRegimeMetric
                    {
                        Regime = predominantRegime
                    };
                }

                var rMetric = _regimeMetrics[predominantRegime];
                rMetric.TradeCount += dayTrades.Count;
                rMetric.WinCount += dayTrades.Count(t => t.NetPnl > 0);
                rMetric.NetPnl += dayTrades.Sum(t => t.NetPnl);
                rMetric.WinRatePercent = rMetric.TradeCount > 0 ? Math.Round((decimal)rMetric.WinCount / rMetric.TradeCount * 100m, 2) : 0m;
                rMetric.NetExpectancy = rMetric.TradeCount > 0 ? Math.Round(rMetric.NetPnl / rMetric.TradeCount, 2) : 0m;

                decimal gp = dayTrades.Where(t => t.GrossPnl > 0).Sum(t => t.GrossPnl);
                decimal gl = Math.Abs(dayTrades.Where(t => t.GrossPnl < 0).Sum(t => t.GrossPnl));
                rMetric.ProfitFactor = gl > 0 ? Math.Round(gp / gl, 2) : (gp > 0 ? 99.9m : 0m);
            }

            UpdateMetricsUnsafe();
        }
    }

    public ForwardValidationStatusDto EvaluateAcceptanceGates()
    {
        lock (_lock)
        {
            UpdateMetricsUnsafe();

            bool passedGates = _status.CurrentNetExpectancy >= _config.MinimumNetExpectancy &&
                               _status.CurrentProfitFactor >= _config.MinimumProfitFactor &&
                               _status.CurrentWinRatePercent >= _config.MinimumWinRatePercent &&
                               _status.CurrentMaxDrawdownPercent <= _config.MaximumPermissibleDrawdownPercent &&
                               _status.MonotonicityScore >= _config.MinimumScoreMonotonicityScore;

            _status.HasPassedAllAcceptanceGates = passedGates;

            if (_status.HasElapsedFullDuration && _status.HasMetSampleCount)
            {
                _status.Status = passedGates ? ValidationCampaignStatus.Completed : ValidationCampaignStatus.Failed;
                _status.FinalDecisionSummary = passedGates
                    ? $"Forward Validation PASSED! Verified over {_status.ElapsedCalendarDays} calendar days and {_status.TotalTradesRecorded} trades. Strategy approved for Guarded Live evaluation."
                    : $"Forward Validation REJECTED. Failed one or more statistical quality gates. See detailed metrics.";
            }
            else
            {
                _status.FinalDecisionSummary = $"Validation in progress: {_status.ElapsedCalendarDays}/{_config.RequiredCalendarDays} calendar days elapsed ({_status.TotalTradesRecorded}/{_config.MinimumTradeSamples} trade samples accumulated).";
            }

            return _status;
        }
    }

    private void UpdateMetricsUnsafe()
    {
        var now = DateTime.UtcNow;
        _status.ElapsedCalendarDays = Math.Max(0, (int)(now - _status.StartDateUtc).TotalDays);
        _status.ElapsedMarketSessions = _observedMarketSessions.Count;
        _status.HasElapsedFullDuration = _status.ElapsedCalendarDays >= _config.RequiredCalendarDays;
        _status.HasMetSampleCount = _campaignTrades.Count >= _config.MinimumTradeSamples;

        _status.TotalTradesRecorded = _campaignTrades.Count;
        if (_campaignTrades.Count == 0) return;

        var winners = _campaignTrades.Where(t => t.NetPnl > 0).ToList();
        var losers = _campaignTrades.Where(t => t.NetPnl <= 0).ToList();

        _status.WinningTrades = winners.Count;
        _status.LosingTrades = losers.Count;
        _status.CurrentWinRatePercent = Math.Round((decimal)winners.Count / _campaignTrades.Count * 100m, 2);

        decimal grossProfit = winners.Sum(w => w.GrossPnl);
        decimal grossLoss = Math.Abs(losers.Sum(l => l.GrossPnl));
        _status.CurrentNetPnl = _campaignTrades.Sum(t => t.NetPnl);
        _status.CurrentNetExpectancy = Math.Round(_status.CurrentNetPnl / _campaignTrades.Count, 2);
        _status.CurrentProfitFactor = grossLoss > 0 ? Math.Round(grossProfit / grossLoss, 2) : (grossProfit > 0 ? 99.9m : 0m);

        // Max Drawdown calculation
        decimal peak = 0;
        decimal current = 0;
        decimal maxDd = 0;
        foreach (var t in _campaignTrades)
        {
            current += t.NetPnl;
            if (current > peak) peak = current;
            decimal dd = peak - current;
            if (dd > maxDd) maxDd = dd;
        }
        _status.CurrentMaxDrawdownPercent = Math.Round((maxDd / 500000m) * 100m, 2);

        _status.RegimeBreakdowns = _regimeMetrics.Values.ToList();
        _status.ScoreBuckets = StratifyBuckets(_campaignTrades);
        _status.MonotonicityScore = CalculateMonotonicityScore(_status.ScoreBuckets);
    }

    private List<ScoreBucketMetric> StratifyBuckets(List<V2HypotheticalTradeRecord> trades)
    {
        var buckets = new List<ScoreBucketMetric>();
        int[] minScores = { 60, 70, 80, 90 };
        for (int i = 0; i < minScores.Length; i++)
        {
            int min = minScores[i];
            int max = i == minScores.Length - 1 ? 100 : minScores[i + 1];
            var bTrades = trades.Where(t => t.EntryConvictionScore >= min && t.EntryConvictionScore <= max).ToList();
            var m = new ScoreBucketMetric
            {
                BucketRange = $"{min} to {max}",
                TradesCount = bTrades.Count,
                WinningTrades = bTrades.Count(t => t.NetPnl > 0),
                WinRatePercent = bTrades.Count > 0 ? Math.Round((decimal)bTrades.Count(t => t.NetPnl > 0) / bTrades.Count * 100m, 2) : 0m,
                NetPnl = bTrades.Sum(t => t.NetPnl),
                NetExpectancyRupees = bTrades.Count > 0 ? Math.Round(bTrades.Sum(t => t.NetPnl) / bTrades.Count, 2) : 0m
            };
            buckets.Add(m);
        }
        return buckets;
    }

    private decimal CalculateMonotonicityScore(List<ScoreBucketMetric> buckets)
    {
        if (buckets == null || buckets.Count < 2) return 1.0m;
        // Count pairs where higher score bucket has >= win rate of lower bucket
        int monotonicPairs = 0;
        int totalPairs = 0;
        for (int i = 0; i < buckets.Count - 1; i++)
        {
            if (buckets[i].TradesCount > 0 && buckets[i + 1].TradesCount > 0)
            {
                totalPairs++;
                if (buckets[i + 1].WinRatePercent >= buckets[i].WinRatePercent) monotonicPairs++;
            }
        }
        return totalPairs > 0 ? Math.Round((decimal)monotonicPairs / totalPairs, 2) : 1.0m;
    }
}
