using AutoFno.Application.Exceptions;
using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.Services;

public class FnOOrderService : IFnOOrderService
{
    private readonly ITradeRepository _tradeRepo;
    private readonly IFnORiskManager _riskManager;
    private readonly IFnOStrategyRepository _strategyRepo;
    private readonly IUpstoxClient _upstoxClient;
    private readonly IUpstoxAuthService _authService;
    private readonly ITradingNotificationService _notificationService;
    private readonly ILogger<FnOOrderService> _logger;

    public FnOOrderService(
        ITradeRepository tradeRepo,
        IFnORiskManager riskManager,
        IFnOStrategyRepository strategyRepo,
        IUpstoxClient upstoxClient,
        IUpstoxAuthService authService,
        ITradingNotificationService notificationService,
        ILogger<FnOOrderService> logger)
    {
        _tradeRepo = tradeRepo;
        _riskManager = riskManager;
        _strategyRepo = strategyRepo;
        _upstoxClient = upstoxClient;
        _authService = authService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<OrderResultDto> PlaceOrderAsync(PlaceFnoOrderRequest request, CancellationToken ct = default)
    {
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString("N");
        request.CorrelationId = correlationId;

        // Idempotency check
        var existing = await _tradeRepo.GetOrderByCorrelationIdAsync(correlationId, ct);
        if (existing != null)
        {
            _logger.LogWarning("Duplicate order attempt detected for CorrelationId {Id}", correlationId);
            return new OrderResultDto
            {
                Success = existing.Status == OrderStatus.Complete,
                OrderId = existing.BrokerOrderId ?? existing.Id.ToString(),
                CorrelationId = correlationId,
                ExecutedPrice = existing.ExecutedPrice,
                Status = existing.Status,
                Message = "Order already submitted (idempotency preserved)."
            };
        }

        var config = await _strategyRepo.GetBotConfigAsync(ct);

        // Risk checks
        await _riskManager.CanExecuteNewTradeAsync(request, config, ct);

        // Resolve Quantity based on Lots if Quantity is 0
        if (request.Quantity <= 0)
        {
            var lotSize = await _riskManager.ResolveLotSizeAsync(request.IndexSymbol, ct);
            var lots = Math.Max(1, request.Lots > 0 ? request.Lots : config.DefaultLots);
            request.Lots = lots;
            request.Quantity = lots * lotSize;
        }

        // Persist pending order to database
        var order = new TradeOrder
        {
            CorrelationId = correlationId,
            InstrumentKey = request.InstrumentKey,
            TradingSymbol = request.TradingSymbol,
            IndexSymbol = request.IndexSymbol,
            StrikePrice = request.StrikePrice,
            OptionType = request.OptionType,
            TransactionType = request.TransactionType,
            OrderType = request.OrderType,
            ProductType = request.ProductType,
            Quantity = request.Quantity,
            Lots = request.Lots,
            Price = request.Price,
            TriggerPrice = request.TriggerPrice,
            StopLossPrice = request.StopLossPrice,
            TakeProfitPrice = request.TakeProfitPrice,
            Status = OrderStatus.Submitted,
            PlacedByMode = request.PlacedByMode,
            CreatedAtUtc = DateTime.UtcNow
        };

        var orderId = await _tradeRepo.CreateOrderAsync(order, ct);
        order.Id = orderId;

        var auth = await _authService.GetStatusAsync(ct);
        OrderResultDto result;

        if (auth.IsConnected)
        {
            try
            {
                var token = await _authService.GetActiveAccessTokenAsync(ct);
                result = await _upstoxClient.PlaceOrderAsync(token, request, ct);
                result.CorrelationId = correlationId;

                await _tradeRepo.UpdateOrderStatusAsync(
                    orderId,
                    result.OrderId,
                    result.Success ? OrderStatus.Complete : OrderStatus.Rejected,
                    result.Message,
                    result.ExecutedPrice,
                    result.Success ? request.Quantity : 0,
                    ct);

                if (result.Success)
                {
                    await _tradeRepo.AddTradeFillAsync(new TradeFill
                    {
                        OrderId = orderId,
                        FillId = Guid.NewGuid().ToString("N")[..8],
                        FillPrice = result.ExecutedPrice > 0 ? result.ExecutedPrice : request.Price,
                        FillQuantity = request.Quantity,
                        FilledAtUtc = DateTime.UtcNow
                    }, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Live order placement failed: {Message}", ex.Message);
                await _tradeRepo.UpdateOrderStatusAsync(orderId, null, OrderStatus.Failed, ex.Message, 0, 0, ct);
                result = new OrderResultDto
                {
                    Success = false,
                    CorrelationId = correlationId,
                    Status = OrderStatus.Failed,
                    Message = $"Upstox live dispatch error: {ex.Message}"
                };
            }
        }
        else
        {
            // Simulated execution for offline testing
            var execPrice = request.Price > 0 ? request.Price : 120.50m;
            _logger.LogInformation("Simulated order executed for {Symbol} at ₹{Price}", request.TradingSymbol, execPrice);

            await _tradeRepo.UpdateOrderStatusAsync(orderId, $"SIM_{orderId}", OrderStatus.Complete, "Filled (Simulated Mode)", execPrice, request.Quantity, ct);
            await _tradeRepo.AddTradeFillAsync(new TradeFill
            {
                OrderId = orderId,
                FillId = $"FILL_{orderId}",
                FillPrice = execPrice,
                FillQuantity = request.Quantity,
                FilledAtUtc = DateTime.UtcNow
            }, ct);

            // Record Position Snapshot
            var positionQty = request.TransactionType == TransactionType.BUY ? request.Quantity : -request.Quantity;
            await _tradeRepo.SavePositionSnapshotAsync(new PositionSnapshot
            {
                InstrumentKey = request.InstrumentKey,
                TradingSymbol = request.TradingSymbol,
                IndexSymbol = request.IndexSymbol,
                StrikePrice = request.StrikePrice,
                OptionType = request.OptionType,
                Quantity = positionQty,
                Lots = request.Lots,
                AveragePrice = execPrice,
                CurrentLtp = execPrice,
                UnrealizedPnl = 0,
                RealizedPnl = 0,
                SnapshotTimeUtc = DateTime.UtcNow
            }, ct);

            result = new OrderResultDto
            {
                Success = true,
                OrderId = $"SIM_{orderId}",
                CorrelationId = correlationId,
                ExecutedPrice = execPrice,
                Status = OrderStatus.Complete,
                Message = "Simulated Order executed successfully."
            };
        }

        await _notificationService.BroadcastOrderUpdateAsync(result);
        return result;
    }

    public async Task<bool> CancelOrderAsync(long orderId, CancellationToken ct = default)
    {
        var order = await _tradeRepo.GetOrderByIdAsync(orderId, ct);
        if (order == null) return false;

        var auth = await _authService.GetStatusAsync(ct);
        if (auth.IsConnected && !string.IsNullOrEmpty(order.BrokerOrderId))
        {
            var token = await _authService.GetActiveAccessTokenAsync(ct);
            await _upstoxClient.CancelOrderAsync(token, order.BrokerOrderId, ct);
        }

        await _tradeRepo.UpdateOrderStatusAsync(orderId, order.BrokerOrderId, OrderStatus.Cancelled, "Cancelled by user", 0, 0, ct);
        return true;
    }

    public async Task<bool> SquareOffPositionAsync(string instrumentKey, CancellationToken ct = default)
    {
        var positions = await _tradeRepo.GetLatestPositionsAsync(ct);
        var target = positions.FirstOrDefault(p => p.InstrumentKey == instrumentKey);
        if (target == null || target.Quantity == 0) return false;

        var exitTx = target.Quantity > 0 ? TransactionType.SELL : TransactionType.BUY;
        var exitQty = Math.Abs(target.Quantity);

        _logger.LogInformation("Squaring off position {Symbol}: Qty {Qty}, Direction {Dir}", target.TradingSymbol, exitQty, exitTx);

        var req = new PlaceFnoOrderRequest
        {
            InstrumentKey = target.InstrumentKey,
            TradingSymbol = target.TradingSymbol,
            IndexSymbol = target.IndexSymbol,
            StrikePrice = target.StrikePrice,
            OptionType = target.OptionType,
            TransactionType = exitTx,
            OrderType = OrderType.MARKET,
            ProductType = ProductType.I,
            Lots = target.Lots,
            Quantity = exitQty,
            Price = target.CurrentLtp,
            PlacedByMode = BotMode.Manual,
            CorrelationId = $"SQOFF_{Guid.NewGuid():N}"
        };

        var result = await PlaceOrderAsync(req, ct);

        // Update position snapshot to 0
        if (result.Success)
        {
            var realized = (result.ExecutedPrice - target.AveragePrice) * target.Quantity;
            await _tradeRepo.SavePositionSnapshotAsync(new PositionSnapshot
            {
                InstrumentKey = target.InstrumentKey,
                TradingSymbol = target.TradingSymbol,
                IndexSymbol = target.IndexSymbol,
                StrikePrice = target.StrikePrice,
                OptionType = target.OptionType,
                Quantity = 0,
                Lots = 0,
                AveragePrice = 0,
                CurrentLtp = result.ExecutedPrice,
                UnrealizedPnl = 0,
                RealizedPnl = realized,
                SnapshotTimeUtc = DateTime.UtcNow
            }, ct);
        }

        return result.Success;
    }

    public async Task<int> SquareOffAllPositionsAsync(CancellationToken ct = default)
    {
        var positions = await _tradeRepo.GetLatestPositionsAsync(ct);
        int closedCount = 0;

        foreach (var pos in positions.Where(p => p.Quantity != 0))
        {
            var success = await SquareOffPositionAsync(pos.InstrumentKey, ct);
            if (success) closedCount++;
        }

        return closedCount;
    }

    public Task<List<TradeOrder>> GetRecentOrdersAsync(int top = 50, CancellationToken ct = default)
    {
        return _tradeRepo.GetRecentOrdersAsync(top, ct);
    }
}
