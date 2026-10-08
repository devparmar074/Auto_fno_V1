using System.Data;
using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using AutoFno.Domain.Enums;
using Dapper;

namespace AutoFno.Infrastructure.Data.Repositories;

public class TradeRepository : ITradeRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public TradeRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> CreateOrderAsync(TradeOrder order, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var id = await db.ExecuteScalarAsync<long>(
            "dbo.sp_CreateTradeOrder",
            new
            {
                order.CorrelationId,
                order.BrokerOrderId,
                order.InstrumentKey,
                order.TradingSymbol,
                order.IndexSymbol,
                order.StrikePrice,
                OptionType = (int?)order.OptionType,
                TransactionType = (int)order.TransactionType,
                OrderType = (int)order.OrderType,
                ProductType = (int)order.ProductType,
                order.Quantity,
                order.Lots,
                order.Price,
                order.TriggerPrice,
                order.StopLossPrice,
                order.TakeProfitPrice,
                Status = (int)order.Status,
                order.StatusMessage,
                order.ExecutedPrice,
                order.FilledQuantity,
                PlacedByMode = (int)order.PlacedByMode
            },
            commandType: CommandType.StoredProcedure);

        order.Id = id;
        return id;
    }

    public async Task UpdateOrderStatusAsync(
        long orderId,
        string? brokerOrderId,
        OrderStatus status,
        string? statusMessage = null,
        decimal executedPrice = 0,
        int filledQty = 0,
        CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        await db.ExecuteAsync(
            "dbo.sp_UpdateTradeOrderStatus",
            new
            {
                OrderId = orderId,
                BrokerOrderId = brokerOrderId,
                Status = (int)status,
                StatusMessage = statusMessage,
                ExecutedPrice = executedPrice,
                FilledQuantity = filledQty
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<TradeOrder?> GetOrderByIdAsync(long orderId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<TradeOrder>(
            "dbo.sp_GetOrderById",
            new { OrderId = orderId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<TradeOrder?> GetOrderByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<TradeOrder>(
            "dbo.sp_GetOrderByCorrelationId",
            new { CorrelationId = correlationId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<TradeOrder>> GetRecentOrdersAsync(int top = 50, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var results = await db.QueryAsync<TradeOrder>(
            "dbo.sp_GetRecentOrders",
            new { Top = top },
            commandType: CommandType.StoredProcedure);
        return results.ToList();
    }

    public async Task<long> AddTradeFillAsync(TradeFill fill, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var id = await db.ExecuteScalarAsync<long>(
            "dbo.sp_AddTradeFill",
            new
            {
                fill.OrderId,
                fill.FillId,
                fill.FillPrice,
                fill.FillQuantity
            },
            commandType: CommandType.StoredProcedure);

        fill.Id = id;
        return id;
    }

    public async Task<List<TradeFill>> GetTradesByOrderIdAsync(long orderId, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var results = await db.QueryAsync<TradeFill>(
            "dbo.sp_GetTradesByOrderId",
            new { OrderId = orderId },
            commandType: CommandType.StoredProcedure);
        return results.ToList();
    }

    public async Task SavePositionSnapshotAsync(PositionSnapshot position, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        await db.ExecuteAsync(
            "dbo.sp_SavePositionSnapshot",
            new
            {
                position.InstrumentKey,
                position.TradingSymbol,
                position.IndexSymbol,
                position.StrikePrice,
                OptionType = (int?)position.OptionType,
                position.Quantity,
                position.Lots,
                position.AveragePrice,
                position.CurrentLtp,
                position.UnrealizedPnl,
                position.RealizedPnl
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<List<PositionSnapshot>> GetLatestPositionsAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var results = await db.QueryAsync<PositionSnapshot>(
            "dbo.sp_GetLatestPositionSnapshots",
            commandType: CommandType.StoredProcedure);
        return results.ToList();
    }
}
