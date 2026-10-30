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

    // Levels 4 through 9 Sub-Engines
    private readonly IHistoricalBacktestEngine _backtestEngine;
    private readonly IShadowPaperTradingEngine _shadowEngine;
    private readonly IForwardValidationEngine _forwardValidationEngine;
    private readonly IGuardedLiveExecutionEngine _guardedLiveEngine;
    private readonly IAutomatedLiveExecutionEngine _automatedLiveEngine;
    private readonly ISelfOptimizingEngine _selfOptimizingEngine;
    private readonly ISystemHealthMonitoringEngine _healthMonitoringEngine;
    private readonly ISystemMaturityDashboardEngine _maturityDashboardEngine;

    private readonly ILogger<ConvictionSystemV2Coordinator> _logger;

    private QuantitativeConvictionV2Config _config = new();
    private V2ConvictionScoreDto _latestScore = new();
    private V2TradeCandidate _latestCandidate = new();
    private DateTime _lastSignalTime = DateTime.MinValue;
    private TradeCandidateDecision _lastDecision = TradeCandidateDecision.NoTrade;
    private readonly List<MLFeatureVector> _featureBuffer = new();
    private readonly object _lock = new();

    public QuantitativeConvictionV2Config Config => _config;
    public IHistoricalBacktestEngine BacktestEngine => _backtestEngine;
    public IShadowPaperTradingEngine ShadowEngine => _shadowEngine;
    public IForwardValidationEngine ForwardValidationEngine => _forwardValidationEngine;
    public IGuardedLiveExecutionEngine GuardedLiveEngine => _guardedLiveEngine;
    public IAutomatedLiveExecutionEngine AutomatedLiveEngine => _automatedLiveEngine;
    public ISelfOptimizingEngine SelfOptimizingEngine => _selfOptimizingEngine;
    public ISystemHealthMonitoringEngine HealthMonitoringEngine => _healthMonitoringEngine;
    public ISystemMaturityDashboardEngine MaturityDashboardEngine => _maturityDashboardEngine;

    // Backward-compatible constructor for existing test suites and lightweight invocations
    public ConvictionSystemV2Coordinator(
        QuantitativeConvictionV2Config config,
        IMarketDataValidationEngine validationEngine,
        IConvictionScoringEngineV2 scoringEngine,
        ITradeQualificationEngine qualificationEngine,
        ITradeManagementEngineV2 tradeManagementEngine,
        IPerformanceAnalyticsEngine analyticsEngine,
        IStrategyComparisonEngine comparisonEngine,
        ILogger<ConvictionSystemV2Coordinator> logger)
        : this(
            config,
            validationEngine,
            scoringEngine,
            qualificationEngine,
            tradeManagementEngine,
            analyticsEngine,
            comparisonEngine,
            new HistoricalBacktestEngine(scoringEngine, qualificationEngine, new RiskManagementEngine(), tradeManagementEngine, new Microsoft.Extensions.Logging.Abstractions.NullLogger<HistoricalBacktestEngine>()),
            new ShadowPaperTradingEngine(config, tradeManagementEngine, analyticsEngine, new Microsoft.Extensions.Logging.Abstractions.NullLogger<ShadowPaperTradingEngine>()),
            new ForwardValidationEngine(new Microsoft.Extensions.Logging.Abstractions.NullLogger<ForwardValidationEngine>()),
            new GuardedLiveExecutionEngine(config, new Microsoft.Extensions.Logging.Abstractions.NullLogger<GuardedLiveExecutionEngine>()),
            new AutomatedLiveExecutionEngine(config, tradeManagementEngine, new Microsoft.Extensions.Logging.Abstractions.NullLogger<AutomatedLiveExecutionEngine>()),
            new SelfOptimizingEngine(new Microsoft.Extensions.Logging.Abstractions.NullLogger<SelfOptimizingEngine>()),
            new SystemHealthMonitoringEngine(config, new Microsoft.Extensions.Logging.Abstractions.NullLogger<SystemHealthMonitoringEngine>()),
            new SystemMaturityDashboardEngine(
                new ForwardValidationEngine(new Microsoft.Extensions.Logging.Abstractions.NullLogger<ForwardValidationEngine>()),
                new ShadowPaperTradingEngine(config, tradeManagementEngine, analyticsEngine, new Microsoft.Extensions.Logging.Abstractions.NullLogger<ShadowPaperTradingEngine>()),
                new SelfOptimizingEngine(new Microsoft.Extensions.Logging.Abstractions.NullLogger<SelfOptimizingEngine>())),
            logger)
    {
    }

    public ConvictionSystemV2Coordinator(
        QuantitativeConvictionV2Config config,
        IMarketDataValidationEngine validationEngine,
        IConvictionScoringEngineV2 scoringEngine,
        ITradeQualificationEngine qualificationEngine,
        ITradeManagementEngineV2 tradeManagementEngine,
        IPerformanceAnalyticsEngine analyticsEngine,
        IStrategyComparisonEngine comparisonEngine,
        IHistoricalBacktestEngine backtestEngine,
        IShadowPaperTradingEngine shadowEngine,
        IForwardValidationEngine forwardValidationEngine,
        IGuardedLiveExecutionEngine guardedLiveEngine,
        IAutomatedLiveExecutionEngine automatedLiveEngine,
        ISelfOptimizingEngine selfOptimizingEngine,
        ISystemHealthMonitoringEngine healthMonitoringEngine,
        ISystemMaturityDashboardEngine maturityDashboardEngine,
        ILogger<ConvictionSystemV2Coordinator> logger)
    {
        _config = config ?? new();
        _validationEngine = validationEngine;
        _scoringEngine = scoringEngine;
        _qualificationEngine = qualificationEngine;
        _tradeManagementEngine = tradeManagementEngine;
        _analyticsEngine = analyticsEngine;
        _comparisonEngine = comparisonEngine;
        _backtestEngine = backtestEngine;
        _shadowEngine = shadowEngine;
        _forwardValidationEngine = forwardValidationEngine;
        _guardedLiveEngine = guardedLiveEngine;
        _automatedLiveEngine = automatedLiveEngine;
        _selfOptimizingEngine = selfOptimizingEngine;
        _healthMonitoringEngine = healthMonitoringEngine;
        _maturityDashboardEngine = maturityDashboardEngine;
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

    public async Task<V2ConvictionScoreDto> EvaluateV2SetupAsync(
        OptionChainDto optionChain,
        PcrSummaryDto pcrSummary,
        MaxPainDto maxPain,
        List<Candle> spotCandles,
        CancellationToken ct = default)
    {
        try
        {
            // Record Market Data Heartbeat in System Health Engine
            if (optionChain != null)
            {
                _healthMonitoringEngine.RecordMarketDataHeartbeat(
                    optionChain.TimestampUtc,
                    optionChain.Strikes?.Count ?? 0);
            }

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
                return invalidScore;
            }

            // 2. Conviction Scoring
            var score = _scoringEngine.CalculateConvictionScore(optionChain, pcrSummary, maxPain, spotCandles, _config);
            _latestScore = score;

            // 3. Trade Qualification
            var activeShadowPositions = await _shadowEngine.GetActivePositionsAsync();
            int openCount = activeShadowPositions.Count;
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

            // 4. ML Feature Extraction
            var featureVector = _selfOptimizingEngine.ExtractFeatures(optionChain, score, spotCandles, score.SpotTrend?.Regime ?? MarketRegime.RangeboundChoppy);
            lock (_lock)
            {
                _featureBuffer.Add(featureVector);
                if (_featureBuffer.Count > 1000) _featureBuffer.RemoveAt(0);
            }

            // 5. Shadow Mode Persistent Processing
            if (_config.V2ShadowMode)
            {
                await _shadowEngine.ProcessLiveSnapshotAsync(optionChain, score, candidate, ct);
            }

            // 6. Safety Cutoffs Check
            await _automatedLiveEngine.CheckAndTriggerSafetyCutoffsAsync(ct);

            return score;
        }
        catch (Exception ex)
        {
            // Absolute Exception Isolation: Never let V2 exceptions bubble up or crash V1
            _logger.LogError(ex, "Unexpected error in ConvictionSystemV2Coordinator. V1 execution remains protected.");
            _healthMonitoringEngine.RegisterAlert($"V2 Coordinator Exception: {ex.Message}");
            return new V2ConvictionScoreDto
            {
                IndexSymbol = optionChain?.UnderlyingSymbol ?? "ERROR",
                SetupRationale = "V2 evaluation encountered an internal error. V1 remains fully operational."
            };
        }
    }

    public V2TradeCandidate GetLatestCandidate() => _latestCandidate;
    public V2ConvictionScoreDto GetLatestScore() => _latestScore;
    public V2PerformanceMetricsDto GetShadowPerformance() => _shadowEngine.GetMetrics();

    public List<V2HypotheticalPosition> GetActiveShadowPositions()
    {
        return _shadowEngine.GetActivePositionsAsync().GetAwaiter().GetResult();
    }

    public V1VsV2ComparisonDto GetComparisonReport(FnOStrategyScoreDto? v1Score)
    {
        var v2Metrics = _shadowEngine.GetMetrics();
        return _comparisonEngine.Compare(v1Score, _latestScore, v2Metrics);
    }
}
