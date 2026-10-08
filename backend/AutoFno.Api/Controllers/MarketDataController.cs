using AutoFno.Application.Interfaces;
using AutoFno.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoFno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly IFnOMarketDataService _marketDataService;

    public MarketDataController(IFnOMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    [HttpGet("spot")]
    public async Task<IActionResult> GetSpot([FromQuery] string? symbol = null, CancellationToken ct = default)
    {
        var ltp = await _marketDataService.GetSpotLtpAsync(symbol, ct);
        return Ok(new { symbol = symbol ?? _marketDataService.ActiveIndex, ltp });
    }

    [HttpGet("candles")]
    public async Task<ActionResult<List<Candle>>> GetCandles(
        [FromQuery] string? symbol = null,
        [FromQuery] string interval = "1minute",
        CancellationToken ct = default)
    {
        var candles = await _marketDataService.GetSpotCandlesAsync(symbol, interval, ct);
        return Ok(candles);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardFnoSummaryDto>> GetDashboard([FromQuery] string? symbol = null, CancellationToken ct = default)
    {
        var summary = await _marketDataService.GetDashboardSummaryAsync(symbol, ct);
        return Ok(summary);
    }
}
