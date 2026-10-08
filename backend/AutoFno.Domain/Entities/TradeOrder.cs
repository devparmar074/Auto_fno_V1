using AutoFno.Domain.Enums;

namespace AutoFno.Domain.Entities;

public class TradeOrder
{
    public long Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? BrokerOrderId { get; set; }
    public string InstrumentKey { get; set; } = string.Empty;
    public string TradingSymbol { get; set; } = string.Empty;
    public string IndexSymbol { get; set; } = "NIFTY50";
    public decimal? StrikePrice { get; set; }
    public OptionType? OptionType { get; set; }
    public TransactionType TransactionType { get; set; }
    public OrderType OrderType { get; set; }
    public ProductType ProductType { get; set; } = ProductType.I;
    public int Quantity { get; set; }
    public int Lots { get; set; } = 1;
    public decimal Price { get; set; }
    public decimal TriggerPrice { get; set; }
    public decimal? StopLossPrice { get; set; }
    public decimal? TakeProfitPrice { get; set; }
    public OrderStatus Status { get; set; }
    public string? StatusMessage { get; set; }
    public decimal ExecutedPrice { get; set; }
    public int FilledQuantity { get; set; }
    public BotMode PlacedByMode { get; set; } = BotMode.Manual;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAtUtc { get; set; }
}
