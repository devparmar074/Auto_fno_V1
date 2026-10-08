using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using AutoFno.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoFno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FnOTradeController : ControllerBase
{
    private readonly IFnOOrderService _orderService;
    private readonly IPortfolioService _portfolioService;

    public FnOTradeController(
        IFnOOrderService orderService,
        IPortfolioService portfolioService)
    {
        _orderService = orderService;
        _portfolioService = portfolioService;
    }

    [HttpPost("order")]
    public async Task<ActionResult<OrderResultDto>> PlaceOrder([FromBody] PlaceFnoOrderRequest request, CancellationToken ct)
    {
        var result = await _orderService.PlaceOrderAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("cancel/{orderId}")]
    public async Task<IActionResult> CancelOrder(long orderId, CancellationToken ct)
    {
        var success = await _orderService.CancelOrderAsync(orderId, ct);
        return Ok(new { success });
    }

    public record SquareOffRequest(string InstrumentKey);

    [HttpPost("square-off")]
    public async Task<IActionResult> SquareOff([FromBody] SquareOffRequest req, CancellationToken ct)
    {
        var success = await _orderService.SquareOffPositionAsync(req.InstrumentKey, ct);
        return Ok(new { success });
    }

    [HttpPost("square-off-all")]
    public async Task<IActionResult> SquareOffAll(CancellationToken ct)
    {
        var count = await _orderService.SquareOffAllPositionsAsync(ct);
        return Ok(new { closedCount = count });
    }

    [HttpGet("positions")]
    public async Task<ActionResult<List<PositionDto>>> GetPositions(CancellationToken ct)
    {
        var positions = await _portfolioService.GetPositionsAsync(ct);
        return Ok(positions);
    }

    [HttpGet("orders")]
    public async Task<ActionResult<List<TradeOrder>>> GetOrders([FromQuery] int top = 50, CancellationToken ct = default)
    {
        var orders = await _orderService.GetRecentOrdersAsync(top, ct);
        return Ok(orders);
    }
}
