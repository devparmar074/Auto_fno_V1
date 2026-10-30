using System;
using System.Collections.Generic;

namespace AutoFno.Domain.V2;

public class ComponentHealthItem
{
    public string ComponentName { get; set; } = string.Empty;
    public SystemHealthStatus Status { get; set; } = SystemHealthStatus.Healthy;
    public string Details { get; set; } = "Operational";
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
}

public class SystemHealthReportDto
{
    public SystemHealthStatus OverallStatus { get; set; } = SystemHealthStatus.Healthy;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public double MarketDataAgeSeconds { get; set; }
    public bool IsMarketDataFresh { get; set; }
    public double BrokerApiLatencyMs { get; set; }
    public bool IsBrokerConnected { get; set; }

    public bool IsV1ExecutionServiceRunning { get; set; }
    public bool IsV2CoordinatorActive { get; set; }
    public bool IsShadowTradingEnabled { get; set; }
    public bool IsLiveTradingLocked { get; set; } = true;

    public int OpenShadowPositionsCount { get; set; }
    public decimal DailyLossUtilizedRupees { get; set; }
    public decimal MaxDailyLossLimitRupees { get; set; }
    public int ConsecutiveLossesCount { get; set; }
    public bool IsCircuitBreakerActive { get; set; }

    public List<ComponentHealthItem> Components { get; set; } = new();
    public List<string> ActiveAlerts { get; set; } = new();
}
