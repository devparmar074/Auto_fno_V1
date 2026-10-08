using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoFno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FnOStrategyController : ControllerBase
{
    private readonly IFnOMarketDataService _marketDataService;
    private readonly IFnOStrategyEngine _strategyEngine;
    private readonly IFnOStrategyRepository _strategyRepo;
    private readonly IFnORiskManager _riskManager;

    public FnOStrategyController(
        IFnOMarketDataService marketDataService,
        IFnOStrategyEngine strategyEngine,
        IFnOStrategyRepository strategyRepo,
        IFnORiskManager riskManager)
    {
        _marketDataService = marketDataService;
        _strategyEngine = strategyEngine;
        _strategyRepo = strategyRepo;
        _riskManager = riskManager;
    }

    [HttpGet("score")]
    public async Task<ActionResult<FnOStrategyScoreDto>> GetCurrentScore([FromQuery] string? symbol = null, CancellationToken ct = default)
    {
        var sym = symbol ?? _marketDataService.ActiveIndex;
        var chain = await _marketDataService.GetOptionChainAsync(sym, null, ct);
        var pcr = await _marketDataService.GetPcrSummaryAsync(sym, ct);
        var maxPain = await _marketDataService.GetMaxPainAsync(sym, ct);
        var candles = await _marketDataService.GetSpotCandlesAsync(sym, "1minute", ct);

        var score = await _strategyEngine.EvaluateSetupAsync(chain, pcr, maxPain, candles, ct);
        return Ok(score);
    }

    [HttpGet("signals")]
    public async Task<ActionResult<List<StrategySignalHistory>>> GetRecentSignals(
        [FromQuery] string? symbol = null,
        [FromQuery] int top = 30,
        CancellationToken ct = default)
    {
        var signals = await _strategyRepo.GetRecentSignalsAsync(symbol, top, ct);
        return Ok(signals);
    }

    [HttpGet("config")]
    public async Task<ActionResult<BotConfig>> GetConfig(CancellationToken ct = default)
    {
        var config = await _strategyRepo.GetBotConfigAsync(ct);
        return Ok(config);
    }

    [HttpPost("config")]
    public async Task<ActionResult<BotConfig>> UpdateConfig([FromBody] BotConfig config, CancellationToken ct = default)
    {
        var updated = await _strategyRepo.UpdateBotConfigAsync(config, ct);
        return Ok(updated);
    }

    public record ModeSwitchRequest(BotMode Mode);

    [HttpPost("mode")]
    public async Task<ActionResult<BotConfig>> SetMode([FromBody] ModeSwitchRequest req, CancellationToken ct = default)
    {
        var config = await _strategyRepo.GetBotConfigAsync(ct);
        config.Mode = req.Mode;
        var updated = await _strategyRepo.UpdateBotConfigAsync(config, ct);
        return Ok(updated);
    }

    [HttpPost("kill-switch")]
    public async Task<IActionResult> TriggerKillSwitch(CancellationToken ct = default)
    {
        await _riskManager.TriggerEmergencyKillSwitchAsync(ct);
        return Ok(new { success = true, message = "Emergency Kill Switch Activated! All trading disarmed." });
    }
}
