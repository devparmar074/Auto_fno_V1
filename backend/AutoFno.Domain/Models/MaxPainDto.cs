namespace AutoFno.Domain.Models;

public class MaxPainDto
{
    public decimal MaxPainStrike { get; set; }
    public decimal TotalPainValue { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal DistanceFromSpot { get; set; }
    public decimal DistancePercent { get; set; }
    public string ExpiryDate { get; set; } = string.Empty;
    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
}
