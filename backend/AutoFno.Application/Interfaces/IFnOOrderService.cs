using AutoFno.Domain.Entities;
using AutoFno.Domain.Models;

namespace AutoFno.Application.Interfaces;

public interface IFnOOrderService
{
    Task<OrderResultDto> PlaceOrderAsync(PlaceFnoOrderRequest request, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(long orderId, CancellationToken ct = default);
    Task<bool> SquareOffPositionAsync(string instrumentKey, CancellationToken ct = default);
    Task<int> SquareOffAllPositionsAsync(CancellationToken ct = default);
    Task<List<TradeOrder>> GetRecentOrdersAsync(int top = 50, CancellationToken ct = default);
}
