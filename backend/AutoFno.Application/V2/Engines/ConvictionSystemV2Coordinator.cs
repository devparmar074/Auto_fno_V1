using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.V2.Engines;

public class ConvictionSystemV2Coordinator : IConvictionSystemV2Coordinator
{
    private readonly IMarketDataValidationEngine _validationEngine;
    private readonly IConvictionScoringEngineV2 _scoringEngine;
    private readonly ITradeQualificationEngine _qualificationEngine;
    private readonly ITradeManagementEngineV2 _tradeManagementEngine;
    private readonly IPerformanceAnalyticsEngine _analyticsEngine;
    private readonly IStrategyComparisonEngine _comparisonEngine;
    private readonly ILogger<ConvictionSystemV2Coordinator> _logger;

    private QuantitativeConvictionV2Config _config = new();
    private V2ConvictionScoreDto _latestScore = new();
    private V2TradeCandidate _latestCandidate = new();
    private DateTime _lastSignalTime = DateTime.MinValue;
    private TradeCandidateDecision _lastDecision = TradeCandidateDecision.NoTrade;
    private readonly List<V2HypotheticalPosition> _activePositions = new();
    private readonly object _positionLock = new();

    public QuantitativeConvictionV2Config Config => _config;

    public ConvictionSystemV2Coordinator(
        QuantitativeConvictionV2Config config,
        IMarketDataValidationEngine validationEngine,
        IConvictionScoringEngineV2 scoringEngine,
        ITradeQualificationEngine qualificationEngine,
        ITradeManagementEngineV2 tradeManagementEngine,
        IPerformanceAnalyticsEngine analyticsEngine,
        IStrategyComparisonEngine comparisonEngine,
        ILogger<ConvictionSystemV2Coordinator> logger)
    {
        _config = config ?? new();
        _validationEngine = validationEngine;
        _scoringEngine = scoringEngine;
        _qualificationEngine = qualificationEngine;
        _tradeManagementEngine = tradeManagementEngine;
        _analyticsEngine = analyticsEngine;
        _comparisonEngine = comparisonEngine;
        _logger = logger;
    }

    public void UpdateConfig(QuantitativeConvictionV2Config newConfig)
    {
        // Enforce strict safety guardrails: V2 Live Trading is strictly forbidden by policy
        if (newConfig.V2LiveTrading)
        {
            _logger.LogWarning("Attempted to enable V2LiveTrading. Rejected by safety policy: V2 Live Mode is unapproved for live execution.");
            newConfig.V2LiveTrading = false;
        }
        _config = newConfig;
    }

    public Task<V2ConvictionScoreDto> EvaluateV2SetupAsync(
        OptionChainDto optionChain,
        PcrSummaryDto pcrSummary,
        MaxPainDto maxPain,
        List<Candle> spotCandles,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Data Validation
            if (!_validationEngine.ValidateMarketData(optionChain, spotCandles, out var errors))
            {
                _logger.LogWarning("V2 Market Data Validation failed: {Errors}", string.Join("; ", errors));
                var invalidScore = new V2ConvictionScoreDto
                {
                    IndexSymbol = optionChain?.UnderlyingSymbol ?? "UNKNOWN",
                    SpotPrice = optionChain?.SpotPrice ?? 0m,
                    SetupRationale = $"Data validation failed: {string.Join("; ", errors)}"
                };
                _latestScore = invalidScore;
                return Task.FromResult(invalidScore);
            }

            // 2. Conviction Scoring
            var score = _scoringEngine.CalculateConvictionScore(optionChain, pcrSummary, maxPain, spotCandles, _config);
            _latestScore = score;

            // 3. Trade Qualification
            int openCount;
            lock (_positionLock)
            {
                openCount = _activePositions.Count(p => p.IsOpen);
            }

            var metrics = _analyticsEngine.GetPerformanceMetrics();
            var candidate = _qualificationEngine.QualifyTrade(
                score, 
                optionChain, 
                spotCandles, 
                _config, 
                openCount, 
                metrics.NetPnl, 
                metrics.MaxConsecutiveLosses);

            var now = DateTime.UtcNow;
            if (candidate.IsQualified)
            {
                if (_lastDecision == candidate.Decision && (now - _lastSignalTime).TotalMinutes < _config.CooldownMinutesAfterLoss)
                {
                    candidate.IsQualified = false;
                    candidate.RejectionReasons |= QualificationRejectionReason.CooldownActive;
                    candidate.RejectionDetails.Add($"Cooldown active: duplicate signal for {_lastDecision} rejected within {_config.CooldownMinutesAfterLoss}m cooldown window.");
                    candidate.Decision = TradeCandidateDecision.NoTrade;
                }
                else
                {
                    _lastSignalTime = now;
                    _lastDecision = candidate.Decision;
                }
            }

            _latestCandidate = candidate;
            _analyticsEngine.RecordSignal(score, candidate);

            // 4. Shadow Mode Simulation Execution
            if (_config.V2ShadowMode)
            {
                ManageShadowPositions(optionChain, candidate);
            }

            return Task.FromResult(score);
        }
        catch (Exception ex)
        {
            // Absolute Exception Isolation: Never let V2 exceptions bubble up or crash V1
            _logger.LogError(ex, "Unexpected error in ConvictionSystemV2Coordinator. V1 execution remains protected.");
            return Task.FromResult(new V2ConvictionScoreDto
            {
                IndexSymbol = optionChain?.UnderlyingSymbol ?? "ERROR",
                SetupRationale = "V2 evaluation encountered an internal error. V1 remains fully operational."
            });
        }
    }

    private void ManageShadowPositions(OptionChainDto optionChain, V2TradeCandidate candidate)
    {
        lock (_positionLock)
        {
            var now = DateTime.UtcNow;

            // 1. Update existing open positions
            for (int i = _activePositions.Count - 1; i >= 0; i--)
            {
                var pos = _activePositions[i];
                if (!pos.IsOpen) continue;

                // Find current LTP of position's option
                var strikeObj = optionChain.Strikes?.FirstOrDefault(s => s.StrikePrice == pos.TargetStrike);
                decimal currentOptionPrice = pos.CurrentPrice;

                if (strikeObj != null)
                {
                    var contract = pos.OptionType == OptionType.CE ? strikeObj.Call : strikeObj.Put;
                    if (contract != null && contract.Ltp > 0)
                    {
                        currentOptionPrice = contract.Ltp;
                    }
                }

                _tradeManagementEngine.UpdatePosition(pos, optionChain.SpotPrice, currentOptionPrice, now, _config, out var exitReason);

                if (exitReason != V2ExitReason.None)
                {
                    var closedRecord = _tradeManagementEngine.ClosePosition(pos, currentOptionPrice, exitReason, now, _config);
                    _analyticsEngine.RecordTrade(closedRecord);
                    _activePositions.RemoveAt(i);
                    _logger.LogInformation("V2 Shadow Trade Closed: {Symbol} {Option} Exit={ExitPrice} PnL={NetPnl} Reason={Reason}",
                        closedRecord.TradingSymbol, closedRecord.OptionType, closedRecord.ExitPrice, closedRecord.NetPnl, closedRecord.ExitReason);
                }
            }

            // 2. Open new shadow position if qualified and within capacity
            if (candidate.IsQualified && candidate.AllocatedLots > 0 && _activePositions.Count(p => p.IsOpen) < _config.MaxSimultaneousPositions)
            {
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
                    EntryPrice = candidate.ExpectedEntryPrice,
                    CurrentPrice = candidate.ExpectedEntryPrice,
                    PeakPrice = candidate.ExpectedEntryPrice,
                    TroughPrice = candidate.ExpectedEntryPrice,
                    StopLossPrice = candidate.StopLossPrice,
                    Target1Price = candidate.Target1Price,
                    Target2Price = candidate.Target2Price,
                    InitialRiskPerLot = (candidate.ExpectedEntryPrice - candidate.StopLossPrice) + candidate.EstimatedCostPerLot,
                    EntryTimeUtc = now,
                    LastUpdatedUtc = now,
                    IsOpen = true
                };

                _activePositions.Add(newPos);
                _logger.LogInformation("V2 Shadow Position Opened: {Symbol} {Lots} Lots @ Rs {Price} (SL: {SL}, TP: {TP})",
                    newPos.TradingSymbol, newPos.Lots, newPos.EntryPrice, newPos.StopLossPrice, newPos.Target1Price);
            }
        }
    }

    public V2TradeCandidate GetLatestCandidate() => _latestCandidate;
    public V2ConvictionScoreDto GetLatestScore() => _latestScore;
    public V2PerformanceMetricsDto GetShadowPerformance() => _analyticsEngine.GetPerformanceMetrics();

    public List<V2HypotheticalPosition> GetActiveShadowPositions()
    {
        lock (_positionLock)
        {
            return _activePositions.Where(p => p.IsOpen).ToList();
        }
    }

    public V1VsV2ComparisonDto GetComparisonReport(FnOStrategyScoreDto? v1Score)
    {
        var v2Metrics = _analyticsEngine.GetPerformanceMetrics();
        return _comparisonEngine.Compare(v1Score, _latestScore, v2Metrics);
    }
}
