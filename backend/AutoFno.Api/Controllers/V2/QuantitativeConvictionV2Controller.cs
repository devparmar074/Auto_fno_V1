using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFno.Application.Interfaces;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AutoFno.Api.Controllers.V2;

[ApiController]
[Route("api/v2/conviction")]
public class QuantitativeConvictionV2Controller : ControllerBase
{
    private readonly IConvictionSystemV2Coordinator _coordinator;
    private readonly IFnOMarketDataService _marketDataService;
    private readonly IFnOStrategyEngine _v1StrategyEngine;
    private readonly ILogger<QuantitativeConvictionV2Controller> _logger;

    public QuantitativeConvictionV2Controller(
        IConvictionSystemV2Coordinator coordinator,
        IFnOMarketDataService marketDataService,
        IFnOStrategyEngine v1StrategyEngine,
        ILogger<QuantitativeConvictionV2Controller> logger)
    {
        _coordinator = coordinator;
        _marketDataService = marketDataService;
        _v1StrategyEngine = v1StrategyEngine;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current V2 Conviction Score (-100 to +100) across all 4 pillars.
    /// </summary>
    [HttpGet("score")]
    public async Task<ActionResult<V2ConvictionScoreDto>> GetCurrentScore(
        [FromQuery] string? symbol = null,
        [FromQuery] bool refresh = false,
        CancellationToken ct = default)
    {
        var latest = _coordinator.GetLatestScore();
        if (!refresh && latest.TimestampUtc > DateTime.UtcNow.AddSeconds(-30) && !string.IsNullOrEmpty(latest.IndexSymbol))
        {
            return Ok(latest);
        }

        var sym = symbol ?? _marketDataService.ActiveIndex;
        var chain = await _marketDataService.GetOptionChainAsync(sym, null, ct);
        var pcr = await _marketDataService.GetPcrSummaryAsync(sym, ct);
        var maxPain = await _marketDataService.GetMaxPainAsync(sym, ct);
        var candles = await _marketDataService.GetSpotCandlesAsync(sym, "1minute", ct);

        var score = await _coordinator.EvaluateV2SetupAsync(chain, pcr, maxPain, candles, ct);
        return Ok(score);
    }

    /// <summary>
    /// Gets the current Trade Candidate with 14-filter qualification checklist and sizing.
    /// </summary>
    [HttpGet("candidate")]
    public ActionResult<V2TradeCandidate> GetLatestCandidate()
    {
        var candidate = _coordinator.GetLatestCandidate();
        return Ok(candidate);
    }

    /// <summary>
    /// Gets hypothetical shadow performance metrics, score buckets, active positions, and trade history.
    /// </summary>
    [HttpGet("shadow/performance")]
    public ActionResult<object> GetShadowPerformance()
    {
        var performance = _coordinator.GetShadowPerformance();
        var activePositions = _coordinator.GetActiveShadowPositions();

        return Ok(new
        {
            Metrics = performance,
            ActivePositions = activePositions,
            ActiveCount = activePositions.Count
        });
    }

    /// <summary>
    /// Side-by-side strategy comparison of V1 vs V2 on identical market data.
    /// </summary>
    [HttpGet("compare")]
    public async Task<ActionResult<V1VsV2ComparisonDto>> GetComparison(
        [FromQuery] string? symbol = null,
        CancellationToken ct = default)
    {
        var sym = symbol ?? _marketDataService.ActiveIndex;
        var chain = await _marketDataService.GetOptionChainAsync(sym, null, ct);
        var pcr = await _marketDataService.GetPcrSummaryAsync(sym, ct);
        var maxPain = await _marketDataService.GetMaxPainAsync(sym, ct);
        var candles = await _marketDataService.GetSpotCandlesAsync(sym, "1minute", ct);

        var v1Score = await _v1StrategyEngine.EvaluateSetupAsync(chain, pcr, maxPain, candles, ct);
        var v2Score = await _coordinator.EvaluateV2SetupAsync(chain, pcr, maxPain, candles, ct);

        var comparison = _coordinator.GetComparisonReport(v1Score);
        return Ok(comparison);
    }

    /// <summary>
    /// Level 4: Executes a historical backtest across provided chronological snapshots.
    /// </summary>
    [HttpPost("backtest/run")]
    public ActionResult<HistoricalBacktestResult> RunHistoricalBacktest([FromBody] BacktestRunPayload payload)
    {
        if (payload?.Snapshots == null || payload.Snapshots.Count == 0)
            return BadRequest("Historical snapshot dataset cannot be null or empty.");

        var request = payload.Request ?? new HistoricalBacktestRequest();
        var result = _coordinator.BacktestEngine.RunBacktest(payload.Snapshots, request);
        return Ok(result);
    }

    /// <summary>
    /// Level 4: Inspects historical data quality (completeness, timestamp continuity, bid/ask consistency).
    /// </summary>
    [HttpPost("backtest/quality")]
    public ActionResult<HistoricalDataQualityReport> InspectDataQuality([FromBody] List<HistoricalMarketSnapshot> snapshots)
    {
        var report = _coordinator.BacktestEngine.InspectDataQuality(snapshots);
        return Ok(report);
    }

    /// <summary>
    /// Level 6: Gets current Forward Validation Campaign status and acceptance gates.
    /// </summary>
    [HttpGet("forward-validation/status")]
    public ActionResult<ForwardValidationStatusDto> GetForwardValidationStatus()
    {
        var status = _coordinator.ForwardValidationEngine.GetCurrentCampaignStatus();
        return Ok(status);
    }

    /// <summary>
    /// Level 6: Starts a fresh Forward Validation Campaign with immutable baseline.
    /// </summary>
    [HttpPost("forward-validation/start")]
    public ActionResult<ForwardValidationStatusDto> StartForwardValidationCampaign([FromBody] ForwardValidationCampaignConfig config)
    {
        var status = _coordinator.ForwardValidationEngine.StartCampaign(config);
        return Ok(status);
    }

    /// <summary>
    /// Level 7: Gets pending execution tickets requiring manual approval.
    /// </summary>
    [HttpGet("guarded/tickets")]
    public ActionResult<List<GuardedExecutionTicket>> GetGuardedTickets()
    {
        var tickets = _coordinator.GuardedLiveEngine.GetPendingTickets();
        return Ok(tickets);
    }

    /// <summary>
    /// Level 7: Submits manual approval or rejection for a guarded candidate ticket.
    /// </summary>
    [HttpPost("guarded/approve")]
    public async Task<ActionResult<GuardedExecutionTicket>> ApproveGuardedCandidate([FromBody] GuardedCandidateApprovalRequest request, CancellationToken ct)
    {
        var ticket = await _coordinator.GuardedLiveEngine.ProcessApprovalAsync(request, ct);
        return Ok(ticket);
    }

    /// <summary>
    /// Level 7: Emergency Kill Switch to immediately freeze all trading and cancel pending orders.
    /// </summary>
    [HttpPost("guarded/kill-switch")]
    public async Task<ActionResult<EmergencyKillSwitchResult>> TriggerEmergencyKillSwitch([FromQuery] string reason, CancellationToken ct)
    {
        var result = await _coordinator.GuardedLiveEngine.TriggerEmergencyKillSwitchAsync(reason ?? "User triggered kill switch via API", ct);
        return Ok(result);
    }

    /// <summary>
    /// Level 8: Gets automated live execution status and circuit breaker states.
    /// </summary>
    [HttpGet("automated/status")]
    public ActionResult<AutomatedExecutionStatusDto> GetAutomatedExecutionStatus()
    {
        var status = _coordinator.AutomatedLiveEngine.GetStatus();
        return Ok(status);
    }

    /// <summary>
    /// Level 8: Reconciles internal active positions against broker positions.
    /// </summary>
    [HttpPost("automated/reconcile")]
    public async Task<ActionResult<BrokerReconciliationReport>> ReconcileBrokerPositions(CancellationToken ct)
    {
        var report = await _coordinator.AutomatedLiveEngine.ReconcileBrokerPositionsAsync(ct);
        return Ok(report);
    }

    /// <summary>
    /// Level 9: Gets active ML model metadata, optimized weights, and versioning status.
    /// </summary>
    [HttpGet("ml/status")]
    public ActionResult<MLModelMetadata> GetMLModelStatus()
    {
        var meta = _coordinator.SelfOptimizingEngine.GetActiveModelMetadata();
        return Ok(meta);
    }

    /// <summary>
    /// Level 9: Runs Walk-Forward ML optimization across genuine observation features.
    /// </summary>
    [HttpPost("ml/train")]
    public async Task<ActionResult<MLTrainingResult>> TrainMLModel([FromBody] List<MLFeatureVector> features, [FromQuery] OptimizationObjective objective = OptimizationObjective.Expectancy, CancellationToken ct = default)
    {
        var result = await _coordinator.SelfOptimizingEngine.RunOptimizationAsync(features, objective, ct);
        return Ok(result);
    }

    /// <summary>
    /// Level 9: Rolls back active scoring weights to the baseline four-pillar model.
    /// </summary>
    [HttpPost("ml/rollback")]
    public ActionResult<object> RollbackMLModel()
    {
        bool success = _coordinator.SelfOptimizingEngine.RollbackToBaseline();
        return Ok(new { Success = success, ActiveModel = _coordinator.SelfOptimizingEngine.GetActiveModelMetadata() });
    }

    /// <summary>
    /// System Health: Retrieves overall health report and telemetry.
    /// </summary>
    [HttpGet("health")]
    public ActionResult<SystemHealthReportDto> GetSystemHealth()
    {
        var health = _coordinator.HealthMonitoringEngine.GetHealthReport();
        return Ok(health);
    }

    /// <summary>
    /// Maturity Dashboard: Retrieves the official Level 0 to Level 9 maturity framework report.
    /// </summary>
    [HttpGet("maturity")]
    public ActionResult<SystemMaturityDashboardDto> GetMaturityDashboard()
    {
        var dashboard = _coordinator.MaturityDashboardEngine.GetDashboard();
        return Ok(dashboard);
    }

    /// <summary>
    /// Gets current Quantitative Conviction V2 configuration and fail-safe parameters.
    /// </summary>
    [HttpGet("config")]
    public ActionResult<QuantitativeConvictionV2Config> GetConfig()
    {
        return Ok(_coordinator.Config);
    }

    /// <summary>
    /// Updates runtime V2 configuration. Live trading cannot be enabled without explicit administrative verification.
    /// </summary>
    [HttpPost("config")]
    public ActionResult<QuantitativeConvictionV2Config> UpdateConfig([FromBody] QuantitativeConvictionV2Config newConfig)
    {
        if (newConfig == null)
            return BadRequest("Configuration cannot be null.");

        _coordinator.UpdateConfig(newConfig);
        _logger.LogInformation("V2 Configuration updated successfully. EnableV2={EnableV2}, ShadowMode={ShadowMode}, LiveTrading={LiveTrading}",
            newConfig.EnableV2, newConfig.V2ShadowMode, newConfig.V2LiveTrading);

        return Ok(_coordinator.Config);
    }
}

public class BacktestRunPayload
{
    public List<HistoricalMarketSnapshot> Snapshots { get; set; } = new();
    public HistoricalBacktestRequest? Request { get; set; }
}
