using System;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoFno.Tests.V2;

public class SystemHealthMonitoringTests
{
    private readonly SystemHealthMonitoringEngine _engine;
    private readonly QuantitativeConvictionV2Config _config;

    public SystemHealthMonitoringTests()
    {
        _config = new QuantitativeConvictionV2Config
        {
            EnableV2 = true,
            V2ShadowMode = true,
            V2LiveTrading = false,
            MaxDailyLossLimit = 10000m
        };
        _engine = new SystemHealthMonitoringEngine(_config, new NullLogger<SystemHealthMonitoringEngine>());
    }

    [Fact]
    public void GetHealthReport_WithRecentHeartbeat_ShouldReportHealthy()
    {
        _engine.RecordMarketDataHeartbeat(DateTime.UtcNow, 25);
        _engine.RecordBrokerApiHeartbeat(45.2, true);

        var report = _engine.GetHealthReport();

        Assert.Equal(SystemHealthStatus.Healthy, report.OverallStatus);
        Assert.True(report.IsMarketDataFresh);
        Assert.True(report.IsBrokerConnected);
        Assert.True(report.IsLiveTradingLocked);
        Assert.True(report.BrokerApiLatencyMs > 0);
    }

    [Fact]
    public void GetHealthReport_WithStaleMarketData_ShouldFlagDegraded()
    {
        // 90 seconds old data
        _engine.RecordMarketDataHeartbeat(DateTime.UtcNow.AddSeconds(-90), 20);

        var report = _engine.GetHealthReport();

        Assert.Equal(SystemHealthStatus.Degraded, report.OverallStatus);
        Assert.False(report.IsMarketDataFresh);
    }

    [Fact]
    public void RegisterAlert_ShouldEnforceFifoCapacityAndLog()
    {
        for (int i = 0; i < 60; i++)
        {
            _engine.RegisterAlert($"Alert #{i}");
        }

        var report = _engine.GetHealthReport();

        Assert.NotEmpty(report.ActiveAlerts);
        Assert.True(report.ActiveAlerts.Count <= 50);
        Assert.Contains(report.ActiveAlerts, a => a.Contains("Alert #59"));
    }
}
