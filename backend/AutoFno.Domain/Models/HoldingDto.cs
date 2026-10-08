namespace AutoFno.Domain.Models;

public class HoldingDto
{
    public string TradingSymbol { get; set; } = string.Empty;
    public string InstrumentKey { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentLtp { get; set; }
    public decimal Pnl { get; set; }
    public decimal ClosePrice { get; set; }
}
