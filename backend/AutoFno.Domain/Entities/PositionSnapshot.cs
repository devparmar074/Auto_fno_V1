using AutoFno.Domain.Enums;

namespace AutoFno.Domain.Entities;

public class PositionSnapshot
{
    public long Id { get; set; }
    public string InstrumentKey { get; set; } = string.Empty;
    public string TradingSymbol { get; set; } = string.Empty;
    public string IndexSymbol { get; set; } = "NIFTY50";
    public decimal? StrikePrice { get; set; }
    public OptionType? OptionType { get; set; }
    public int Quantity { get; set; }
    public int Lots { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentLtp { get; set; }
    public decimal UnrealizedPnl { get; set; }
    public decimal RealizedPnl { get; set; }
    public DateTime SnapshotTimeUtc { get; set; } = DateTime.UtcNow;
}
