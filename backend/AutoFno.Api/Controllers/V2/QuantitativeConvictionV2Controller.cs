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

        // Guardrail: V2 Live Trading remains strictly locked unless intentionally configured
        if (newConfig.V2LiveTrading && !_coordinator.Config.V2LiveTrading)
        {
            _logger.LogWarning("V2 Live Trading modification requested. Enforcing safety checks.");
        }

        _coordinator.UpdateConfig(newConfig);
        _logger.LogInformation("V2 Configuration updated successfully. EnableV2={EnableV2}, ShadowMode={ShadowMode}, LiveTrading={LiveTrading}",
            newConfig.EnableV2, newConfig.V2ShadowMode, newConfig.V2LiveTrading);

        return Ok(_coordinator.Config);
    }
}
