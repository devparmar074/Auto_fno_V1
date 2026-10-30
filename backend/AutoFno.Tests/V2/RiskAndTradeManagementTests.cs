using System;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.V2;
using Xunit;

namespace AutoFno.Tests.V2;

public class RiskAndTradeManagementTests
{
    private readonly RiskManagementEngine _riskEngine = new();
    private readonly TradeManagementEngine _tradeEngine = new();
    private readonly QuantitativeConvictionV2Config _config = new();

    [Fact]
    public void Position_Sizing_Never_Exceeds_Risk_Limit()
    {
        // Capital = 500,000, RiskPerTrade = 0.5% (Rs 2,500)
        // Entry = 100, SL = 80 -> Risk per point = 20 -> Per lot (25 qty) = 500 pts + costs
        var candidate = new V2TradeCandidate
        {
            IndexSymbol = "NIFTY50",
            ExpectedEntryPrice = 100m,
            StopLossPrice = 80m,
            EstimatedCostPerLot = 50m
        };

        _riskEngine.CalculatePositionSizing(candidate, _config, todayRealizedLoss: 0m);

        decimal maxAllowedRisk = _config.AccountCapital * (_config.RiskPerTradePercent / 100m); // 2,500
        Assert.True(candidate.AllocatedLots > 0);
        Assert.True(candidate.TotalRiskRupees <= maxAllowedRisk, $"Total risk {candidate.TotalRiskRupees} exceeds max allowed {maxAllowedRisk}");
    }

    [Fact]
    public void Daily_Loss_Cap_Enforces_Trading_Halt()
    {
        var candidate = new V2TradeCandidate { IndexSymbol = "NIFTY50" };

        // Realized loss within limits
        bool permittedUnderLimit = _riskEngine.IsRiskPermitted(candidate, _config, 0, todayRealizedPnl: -4000m, consecutiveLosses: 0, out var reason1);
        Assert.True(permittedUnderLimit);
        Assert.Empty(reason1);

        // Realized loss hits or exceeds daily limit (-Rs 5000)
        bool permittedExceeded = _riskEngine.IsRiskPermitted(candidate, _config, 0, todayRealizedPnl: -5001m, consecutiveLosses: 0, out var reason2);
        Assert.False(permittedExceeded);
        Assert.Contains("Daily loss lock active", reason2);
    }

    [Fact]
    public void Stop_Loss_Triggers_When_Price_Hits_Or_Breaches_SL()
    {
        var pos = new V2HypotheticalPosition
        {
            TradingSymbol = "NIFTY24500CE",
            OptionType = OptionType.CE,
            Lots = 2,
            Quantity = 50,
            RemainingQuantity = 50,
            EntryPrice = 100m,
            CurrentPrice = 100m,
            StopLossPrice = 85m,
            Target1Price = 130m,
            Target2Price = 160m,
            IsOpen = true,
            EntryTimeUtc = DateTime.UtcNow
        };

        // Price drops to 84 (below SL 85)
        _tradeEngine.UpdatePosition(pos, currentSpot: 24400m, currentOptionPrice: 84m, DateTime.UtcNow, _config, out var exitReason);

        Assert.Equal(V2ExitReason.EmergencyLossLock, exitReason);
    }

    [Fact]
    public void Target_1_Partial_Exit_Closes_Half_And_Moves_Stop_To_Breakeven()
    {
        var pos = new V2HypotheticalPosition
        {
            TradingSymbol = "NIFTY24500CE",
            OptionType = OptionType.CE,
            Lots = 4,
            Quantity = 100,
            RemainingQuantity = 100,
            EntryPrice = 100m,
            CurrentPrice = 100m,
            StopLossPrice = 85m,
            Target1Price = 130m,
            Target2Price = 160m,
            IsOpen = true,
            EntryTimeUtc = DateTime.UtcNow
        };

        // Price hits Target 1 at 116 (+1.07R milestone reached) during market hours (10:00 AM IST)
        var marketTime = new DateTime(2026, 10, 30, 4, 30, 0, DateTimeKind.Utc);
        _tradeEngine.UpdatePosition(pos, currentSpot: 24650m, currentOptionPrice: 116m, marketTime, _config, out var exitReason);

        Assert.Equal(V2ExitReason.None, exitReason); // Full position is NOT closed yet, runner continues
        Assert.True(pos.IsPartiallyExited);
        Assert.Equal(50, pos.RemainingQuantity); // 50% closed
        Assert.Equal(100m, pos.StopLossPrice);   // SL moved to breakeven
        Assert.True(pos.IsOpen);                  // Position remains open for runner to TP2
    }

    [Fact]
    public void Target_2_Exits_Remaining_Position_Completely()
    {
        var pos = new V2HypotheticalPosition
        {
            TradingSymbol = "NIFTY24500CE",
            OptionType = OptionType.CE,
            Lots = 4,
            Quantity = 100,
            RemainingQuantity = 50,
            IsPartiallyExited = true,
            EntryPrice = 100m,
            CurrentPrice = 130m,
            StopLossPrice = 100m,
            Target1Price = 130m,
            Target2Price = 160m,
            IsOpen = true,
            EntryTimeUtc = DateTime.UtcNow
        };

        // Price advances to Target 2 at 162
        var marketTime = new DateTime(2026, 10, 30, 4, 30, 0, DateTimeKind.Utc);
        _tradeEngine.UpdatePosition(pos, currentSpot: 24800m, currentOptionPrice: 162m, marketTime, _config, out var exitReason);

        Assert.Equal(V2ExitReason.Target2Hit, exitReason);
    }

    [Fact]
    public void Trailing_Stop_Advances_When_Option_Price_Rallies()
    {
        var pos = new V2HypotheticalPosition
        {
            TradingSymbol = "NIFTY24500CE",
            OptionType = OptionType.CE,
            Lots = 2,
            Quantity = 50,
            RemainingQuantity = 50,
            EntryPrice = 100m,
            CurrentPrice = 100m,
            StopLossPrice = 85m,
            Target1Price = 140m,
            Target2Price = 180m,
            IsOpen = true,
            EntryTimeUtc = DateTime.UtcNow
        };

        // Price rallies to 125 (gain of 25 pts, R: 25 / 15 = 1.67R >= 1.5R threshold) during market hours
        var marketTime = new DateTime(2026, 10, 30, 4, 30, 0, DateTimeKind.Utc);
        _tradeEngine.UpdatePosition(pos, currentSpot: 24600m, currentOptionPrice: 125m, marketTime, _config, out var exitReason);

        Assert.Equal(V2ExitReason.None, exitReason);
        Assert.True(pos.StopLossPrice > 85m, "Stop-loss should have trailed upwards");
        Assert.Equal(111.25m, pos.StopLossPrice);
    }

    [Fact]
    public void Time_Based_Cutoff_Exits_Open_Positions_At_Configured_Hour()
    {
        var pos = new V2HypotheticalPosition
        {
            TradingSymbol = "NIFTY24500CE",
            OptionType = OptionType.CE,
            Lots = 2,
            Quantity = 50,
            RemainingQuantity = 50,
            EntryPrice = 100m,
            CurrentPrice = 105m,
            StopLossPrice = 85m,
            Target1Price = 140m,
            Target2Price = 180m,
            IsOpen = true,
            EntryTimeUtc = DateTime.UtcNow.AddHours(-2)
        };

        // 15:16 IST = 09:46 UTC
        var nowUtc = new DateTime(2026, 10, 30, 9, 46, 0, DateTimeKind.Utc);
        _tradeEngine.UpdatePosition(pos, currentSpot: 24550m, currentOptionPrice: 105m, nowUtc, _config, out var exitReason);

        Assert.Equal(V2ExitReason.TimeCutoffExit, exitReason);
    }

    [Fact]
    public void Transaction_Costs_And_Slippage_Deducted_Accurately()
    {
        var pos = new V2HypotheticalPosition
        {
            TradingSymbol = "NIFTY24500CE",
            OptionType = OptionType.CE,
            Lots = 2,
            Quantity = 50,
            RemainingQuantity = 50,
            EntryPrice = 100m,
            CurrentPrice = 120m,
            PeakPrice = 120m,
            TroughPrice = 100m,
            StopLossPrice = 85m,
            IsOpen = true,
            EntryTimeUtc = DateTime.UtcNow.AddMinutes(-30)
        };

        // Exit at 120 (slippage 0.25% gives 119.70 realistic fill)
        var record = _tradeEngine.ClosePosition(pos, 120m, V2ExitReason.Target1Hit, DateTime.UtcNow, _config);

        Assert.Equal(985.00m, record.GrossPnl);
        Assert.True(record.EstimatedCosts > 0m);
        Assert.True(record.NetPnl < record.GrossPnl, "Net PnL must deduct realistic friction");
        Assert.Equal(record.GrossPnl - record.EstimatedCosts, record.NetPnl);
    }
}
