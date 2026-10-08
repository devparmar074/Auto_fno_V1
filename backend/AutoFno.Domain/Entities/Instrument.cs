namespace AutoFno.Domain.Entities;

public class Instrument
{
    public int Id { get; set; }
    public string InstrumentKey { get; set; } = string.Empty;
    public string TradingSymbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE";
    public string Segment { get; set; } = "NSE_FO";
    public string? UnderlyingKey { get; set; }
    public string? UnderlyingSymbol { get; set; }
    public decimal? StrikePrice { get; set; }
    public string? OptionType { get; set; } // CE or PE
    public DateTime? ExpiryDate { get; set; }
    public int LotSize { get; set; } = 1;
    public decimal TickSize { get; set; } = 0.05m;
    public decimal FreezeQuantity { get; set; } = 1800m;
    public string? ExchangeToken { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
