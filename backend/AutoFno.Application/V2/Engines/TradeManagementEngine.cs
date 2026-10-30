using System;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class TradeManagementEngine : ITradeManagementEngineV2
{
    public void UpdatePosition(
        V2HypotheticalPosition position,
        decimal currentSpot,
        decimal currentOptionPrice,
        DateTime currentTimeUtc,
        QuantitativeConvictionV2Config config,
        out V2ExitReason exitReason)
    {
        exitReason = V2ExitReason.None;
        if (!position.IsOpen) return;

        position.CurrentPrice = currentOptionPrice;
        position.LastUpdatedUtc = currentTimeUtc;

        // Track peaks and troughs for MAE / MFE
        if (currentOptionPrice > position.PeakPrice) position.PeakPrice = currentOptionPrice;
        if (currentOptionPrice < position.TroughPrice) position.TroughPrice = currentOptionPrice;

        var riskPerShare = position.EntryPrice - position.StopLossPrice;
        if (riskPerShare <= 0) riskPerShare = position.EntryPrice * 0.20m; // Fallback 20%

        var profitPerShare = currentOptionPrice - position.EntryPrice;
        var currentR = riskPerShare > 0 ? (profitPerShare / riskPerShare) : 0m;

        // 1. Target 2 Hit (Full Take Profit)
        if (currentOptionPrice >= position.Target2Price)
        {
            exitReason = V2ExitReason.Target2Hit;
            return;
        }

        // 2. Target 1 / Partial Profit Milestone (+1R)
        if (!position.IsPartiallyExited && currentR >= config.PartialProfitBookingRMultiple)
        {
            // Book 50% partial profit
            var partialQty = position.RemainingQuantity / 2;
            if (partialQty > 0)
            {
                position.IsPartiallyExited = true;
                position.RemainingQuantity -= partialQty;
                var partialPnl = partialQty * (currentOptionPrice - position.EntryPrice);
                position.RealizedPnl += partialPnl;

                // Move stop to break-even to lock trade risk-free
                position.StopLossPrice = position.EntryPrice;
            }
        }

        // 3. Trailing Stop Milestone (+1.5R to +2.0R)
        if (currentR >= config.TrailingStopActivationRMultiple)
        {
            // Trail stop up to lock at least +0.75R profit
            var trailedStop = position.EntryPrice + (0.75m * riskPerShare);
            if (trailedStop > position.StopLossPrice)
            {
                position.StopLossPrice = Math.Round(trailedStop, 2);
            }
        }

        // 4. Stop Loss / Trailing Stop Triggered
        if (currentOptionPrice <= position.StopLossPrice)
        {
            exitReason = position.IsPartiallyExited || position.StopLossPrice >= position.EntryPrice
                ? V2ExitReason.TrailingStopHit
                : V2ExitReason.EmergencyLossLock;
            return;
        }

        // 5. Time-based Exit: Check intraday cutoff (3:15 PM IST = 09:45 AM UTC)
        var istTime = currentTimeUtc.AddHours(5).AddMinutes(30);
        if (istTime.TimeOfDay >= new TimeSpan(15, 15, 0))
        {
            exitReason = V2ExitReason.TimeCutoffExit;
            return;
        }

        // Compute live unrealized P&L on remaining open quantity
        position.UnrealizedPnl = Math.Round(position.RemainingQuantity * (currentOptionPrice - position.EntryPrice), 2);
    }

    public V2HypotheticalTradeRecord ClosePosition(
        V2HypotheticalPosition position,
        decimal exitPrice,
        V2ExitReason reason,
        DateTime exitTimeUtc,
        QuantitativeConvictionV2Config config)
    {
        position.IsOpen = false;
        position.ExitReason = reason;

        // Realistic execution modeling: apply slippage to exit price
        var slippage = exitPrice * (config.EstimatedSlippagePercent / 100.0m);
        var realisticExitPrice = Math.Max(0.05m, exitPrice - slippage);

        // Calculate Gross P&L on remaining quantity + already realized partial P&L
        var remainingPnl = position.RemainingQuantity * (realisticExitPrice - position.EntryPrice);
        var grossPnl = position.RealizedPnl + remainingPnl;

        // Calculate realistic friction: Brokerage + STT/Turnover + Slippage
        var turnover = (position.EntryPrice + realisticExitPrice) * position.Quantity;
        var taxes = turnover * (config.ExchangeTaxesPercent / 100.0m);
        var brokerage = config.BrokeragePerOrder * 2m; // Entry + Exit orders
        var totalCosts = Math.Round(brokerage + taxes + (slippage * position.Quantity), 2);

        var netPnl = Math.Round(grossPnl - totalCosts, 2);

        var riskPerShare = position.EntryPrice - position.StopLossPrice;
        var rMultiple = (riskPerShare > 0 && position.Quantity > 0)
            ? Math.Round(grossPnl / (riskPerShare * position.Quantity), 2)
            : 0m;

        // MAE & MFE percentage calculation
        var mae = position.EntryPrice > 0 ? ((position.TroughPrice - position.EntryPrice) / position.EntryPrice) * 100m : 0m;
        var mfe = position.EntryPrice > 0 ? ((position.PeakPrice - position.EntryPrice) / position.EntryPrice) * 100m : 0m;

        var holdingMinutes = Math.Round((exitTimeUtc - position.EntryTimeUtc).TotalMinutes, 1);

        return new V2HypotheticalTradeRecord
        {
            TradeId = position.PositionId,
            IndexSymbol = position.IndexSymbol,
            TradingSymbol = position.TradingSymbol,
            OptionType = position.OptionType,
            Lots = position.Lots,
            Quantity = position.Quantity,
            EntryPrice = position.EntryPrice,
            ExitPrice = realisticExitPrice,
            GrossPnl = Math.Round(grossPnl, 2),
            EstimatedCosts = totalCosts,
            NetPnl = netPnl,
            RMultipleAchieved = rMultiple,
            HoldingMinutes = holdingMinutes,
            MaePercent = Math.Round(mae, 2),
            MfePercent = Math.Round(mfe, 2),
            ExitReason = reason,
            EntryConvictionScore = 0m,
            EntryTimeUtc = position.EntryTimeUtc,
            ExitTimeUtc = exitTimeUtc
        };
    }
}
