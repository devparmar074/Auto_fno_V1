using AutoFno.Domain.Enums;

namespace AutoFno.Domain.Entities;

public class BotConfig
{
    public int Id { get; set; }
    public BotMode Mode { get; set; } = BotMode.Manual;
    public string ActiveIndex { get; set; } = "NIFTY50";
    public int DefaultLots { get; set; } = 1;
    public decimal MaxDailyLossLimit { get; set; } = 5000m;
    public decimal MaxDailyProfitTarget { get; set; } = 10000m;
    public decimal StopLossPercent { get; set; } = 20.0m;
    public decimal TakeProfitPercent { get; set; } = 40.0m;
    public int MaxOpenPositions { get; set; } = 2;
    public bool IsKillSwitchActive { get; set; } = false;
    public bool AutoSquareOffAtCutoff { get; set; } = true;
    public string AutoSquareOffTimeIst { get; set; } = "15:15:00";
    public TimeSpan AutoSquareOffTimeSpan => TimeSpan.TryParse(AutoSquareOffTimeIst, out var t) ? t : new TimeSpan(15, 15, 0);
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
