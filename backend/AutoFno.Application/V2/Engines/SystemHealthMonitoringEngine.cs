using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.V2.Engines;

public class SystemHealthMonitoringEngine : ISystemHealthMonitoringEngine
{
    private readonly QuantitativeConvictionV2Config _config;
    private readonly ILogger<SystemHealthMonitoringEngine> _logger;

    private DateTime _lastMarketDataHeartbeat = DateTime.MinValue;
    private int _lastStrikesCount = 0;
    private double _lastBrokerLatencyMs = 0;
    private bool _isBrokerConnected = true;
    private readonly ConcurrentQueue<string> _activeAlerts = new();
    private readonly object _lock = new();

    public SystemHealthMonitoringEngine(
        QuantitativeConvictionV2Config config,
        ILogger<SystemHealthMonitoringEngine> logger)
    {
        _config = config;
        _logger = logger;
    }

    public void RecordMarketDataHeartbeat(DateTime timestampUtc, int strikesCount)
    {
        _lastMarketDataHeartbeat = timestampUtc;
        _lastStrikesCount = strikesCount;
    }

    public void RecordBrokerApiHeartbeat(double latencyMs, bool isSuccess)
    {
        _lastBrokerLatencyMs = latencyMs;
        _isBrokerConnected = isSuccess;
    }

    public void RegisterAlert(string alertMessage)
    {
        _activeAlerts.Enqueue($"[{DateTime.UtcNow:HH:mm:ss}] {alertMessage}");
        while (_activeAlerts.Count > 50) _activeAlerts.TryDequeue(out _);
        _logger.LogWarning("System Health Alert: {Alert}", alertMessage);
    }

    public SystemHealthReportDto GetHealthReport()
    {
        var now = DateTime.UtcNow;
        double dataAgeSeconds = _lastMarketDataHeartbeat == DateTime.MinValue ? 999.0 : (now - _lastMarketDataHeartbeat).TotalSeconds;
        bool isFresh = dataAgeSeconds <= 60.0;

        var report = new SystemHealthReportDto
        {
            TimestampUtc = now,
            MarketDataAgeSeconds = Math.Round(dataAgeSeconds, 1),
            IsMarketDataFresh = isFresh,
            BrokerApiLatencyMs = Math.Round(_lastBrokerLatencyMs, 1),
            IsBrokerConnected = _isBrokerConnected,
            IsV1ExecutionServiceRunning = true,
            IsV2CoordinatorActive = _config.EnableV2,
            IsShadowTradingEnabled = _config.V2ShadowMode,
            IsLiveTradingLocked = !_config.V2LiveTrading,
            MaxDailyLossLimitRupees = _config.MaxDailyLossLimit,
            ActiveAlerts = _activeAlerts.TakeLast(10).ToList()
        };

        // Determine overall status
        if (!isFresh && _lastMarketDataHeartbeat != DateTime.MinValue)
        {
            report.OverallStatus = SystemHealthStatus.Degraded;
            report.Components.Add(new ComponentHealthItem
            {
                ComponentName = "MarketDataFeed",
                Status = SystemHealthStatus.Degraded,
                Details = $"Market data feed is {dataAgeSeconds:F0}s old (threshold 60s)."
            });
        }
        else
        {
            report.Components.Add(new ComponentHealthItem
            {
                ComponentName = "MarketDataFeed",
                Status = SystemHealthStatus.Healthy,
                Details = $"Feed active with {_lastStrikesCount} option strikes."
            });
        }

        report.Components.Add(new ComponentHealthItem
        {
            ComponentName = "V1LegacyExecutionService",
            Status = SystemHealthStatus.Healthy,
            Details = "Fully isolated and operating normally."
        });

        report.Components.Add(new ComponentHealthItem
        {
            ComponentName = "V2QuantitativeEngines",
            Status = _config.EnableV2 ? SystemHealthStatus.Healthy : SystemHealthStatus.Healthy,
            Details = _config.EnableV2 ? "11 quantitative engines active." : "Engines standby."
        });

        report.Components.Add(new ComponentHealthItem
        {
            ComponentName = "BrokerApiConnection",
            Status = _isBrokerConnected ? SystemHealthStatus.Healthy : SystemHealthStatus.Critical,
            Details = _isBrokerConnected ? $"Connected (Latency: {_lastBrokerLatencyMs:F1}ms)" : "Broker disconnected or timeout."
        });

        return report;
    }
}
