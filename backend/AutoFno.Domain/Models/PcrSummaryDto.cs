namespace AutoFno.Domain.Models;

public class PcrSummaryDto
{
    public decimal OiPcr { get; set; }
    public decimal VolumePcr { get; set; }
    public decimal AtmPcr { get; set; }
    public decimal PrevOiPcr { get; set; }
    public decimal PcrSlope { get; set; }
    public string Sentiment { get; set; } = "Neutral";
    public int LongBuildupCount { get; set; }
    public int ShortBuildupCount { get; set; }
    public int ShortCoveringCount { get; set; }
    public int LongUnwindingCount { get; set; }
    public long TotalCallOi { get; set; }
    public long TotalPutOi { get; set; }
    public decimal CallOiChangeTotal { get; set; }
    public decimal PutOiChangeTotal { get; set; }
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
}
