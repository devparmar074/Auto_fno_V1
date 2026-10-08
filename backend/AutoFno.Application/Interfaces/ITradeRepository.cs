using AutoFno.Domain.Entities;
using AutoFno.Domain.Enums;

namespace AutoFno.Application.Interfaces;

public interface ITradeRepository
{
    Task<long> CreateOrderAsync(TradeOrder order, CancellationToken ct = default);
    Task UpdateOrderStatusAsync(long orderId, string? brokerOrderId, OrderStatus status, string? statusMessage = null, decimal executedPrice = 0, int filledQty = 0, CancellationToken ct = default);
    Task<TradeOrder?> GetOrderByIdAsync(long orderId, CancellationToken ct = default);
    Task<TradeOrder?> GetOrderByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
    Task<List<TradeOrder>> GetRecentOrdersAsync(int top = 50, CancellationToken ct = default);
    Task<long> AddTradeFillAsync(TradeFill fill, CancellationToken ct = default);
    Task<List<TradeFill>> GetTradesByOrderIdAsync(long orderId, CancellationToken ct = default);
    Task SavePositionSnapshotAsync(PositionSnapshot position, CancellationToken ct = default);
    Task<List<PositionSnapshot>> GetLatestPositionsAsync(CancellationToken ct = default);
}
