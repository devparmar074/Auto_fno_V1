using AutoFno.Domain.Enums;

namespace AutoFno.Domain.Models;

public class PlaceFnoOrderRequest
{
    public string InstrumentKey { get; set; } = string.Empty;
    public string TradingSymbol { get; set; } = string.Empty;
    public string IndexSymbol { get; set; } = "NIFTY50";
    public decimal? StrikePrice { get; set; }
    public OptionType? OptionType { get; set; }
    public TransactionType TransactionType { get; set; } = TransactionType.BUY;
    public OrderType OrderType { get; set; } = OrderType.MARKET;
    public ProductType ProductType { get; set; } = ProductType.I;
    public int Lots { get; set; } = 1;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TriggerPrice { get; set; }
    public decimal? StopLossPrice { get; set; }
    public decimal? TakeProfitPrice { get; set; }
    public BotMode PlacedByMode { get; set; } = BotMode.Manual;
    public string? CorrelationId { get; set; }
}
