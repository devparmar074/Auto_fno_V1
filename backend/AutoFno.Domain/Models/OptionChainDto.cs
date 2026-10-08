namespace AutoFno.Domain.Models;

public class OptionChainDto
{
    public string UnderlyingKey { get; set; } = string.Empty;
    public string UnderlyingSymbol { get; set; } = "NIFTY50";
    public decimal SpotPrice { get; set; }
    public decimal PrevSpotPrice { get; set; }
    public decimal SpotDayChange { get; set; }
    public decimal SpotDayChangePercent { get; set; }
    public string ExpiryDate { get; set; } = string.Empty;
    public List<string> AvailableExpiries { get; set; } = new();
    public decimal AtmStrike { get; set; }
    public decimal MaxPainStrike { get; set; }
    public decimal CallWallStrike { get; set; } // Highest Call OI (Resistance)
    public decimal PutWallStrike { get; set; }  // Highest Put OI (Support)
    public long TotalCallOi { get; set; }
    public long TotalPutOi { get; set; }
    public long TotalCallVolume { get; set; }
    public long TotalPutVolume { get; set; }
    public decimal OiPcr { get; set; }
    public decimal VolumePcr { get; set; }
    public List<StrikeDataDto> Strikes { get; set; } = new();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
