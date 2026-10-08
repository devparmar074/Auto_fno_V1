using AutoFno.Application.Interfaces;
using AutoFno.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoFno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OptionChainController : ControllerBase
{
    private readonly IFnOMarketDataService _marketDataService;

    public OptionChainController(IFnOMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    [HttpGet("chain")]
    public async Task<ActionResult<OptionChainDto>> GetOptionChain(
        [FromQuery] string? symbol = null,
        [FromQuery] string? expiry = null,
        CancellationToken ct = default)
    {
        var chain = await _marketDataService.GetOptionChainAsync(symbol, expiry, ct);
        return Ok(chain);
    }

    [HttpGet("expiries")]
    public async Task<ActionResult<List<string>>> GetExpiries([FromQuery] string? symbol = null, CancellationToken ct = default)
    {
        var expiries = await _marketDataService.GetAvailableExpiriesAsync(symbol, ct);
        return Ok(expiries);
    }

    [HttpGet("pcr")]
    public async Task<ActionResult<PcrSummaryDto>> GetPcr([FromQuery] string? symbol = null, CancellationToken ct = default)
    {
        var pcr = await _marketDataService.GetPcrSummaryAsync(symbol, ct);
        return Ok(pcr);
    }

    [HttpGet("maxpain")]
    public async Task<ActionResult<MaxPainDto>> GetMaxPain([FromQuery] string? symbol = null, CancellationToken ct = default)
    {
        var maxPain = await _marketDataService.GetMaxPainAsync(symbol, ct);
        return Ok(maxPain);
    }

    public record SwitchIndexRequest(string IndexSymbol);

    [HttpPost("switch-index")]
    public IActionResult SwitchIndex([FromBody] SwitchIndexRequest req)
    {
        _marketDataService.ActiveIndex = req.IndexSymbol.ToUpperInvariant();
        return Ok(new { activeIndex = _marketDataService.ActiveIndex });
    }

    public record SelectExpiryRequest(string ExpiryDate);

    [HttpPost("select-expiry")]
    public IActionResult SelectExpiry([FromBody] SelectExpiryRequest req)
    {
        _marketDataService.SelectedExpiry = req.ExpiryDate;
        return Ok(new { selectedExpiry = _marketDataService.SelectedExpiry });
    }
}
