namespace AutoFno.Domain.Entities;

public class TradeFill
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string? FillId { get; set; }
    public decimal FillPrice { get; set; }
    public int FillQuantity { get; set; }
    public DateTime FilledAtUtc { get; set; } = DateTime.UtcNow;
}
