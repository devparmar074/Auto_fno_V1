using System;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class RiskManagementEngine : IRiskManagementEngineV2
{
    public bool IsRiskPermitted(
        V2TradeCandidate candidate,
        QuantitativeConvictionV2Config config,
        int currentOpenPositionsCount,
        decimal todayRealizedPnl,
        int consecutiveLosses,
        out string rejectionReason)
    {
        rejectionReason = string.Empty;

        // Daily Loss Lock check
        if (todayRealizedPnl <= -config.MaxDailyLossLimit)
        {
            rejectionReason = $"Daily loss lock active: Realized loss Rs {Math.Abs(todayRealizedPnl):N2} has hit/exceeded limit (Rs {config.MaxDailyLossLimit:N2}). All entries frozen.";
            return false;
        }

        // Consecutive Loss Lock check
        if (consecutiveLosses >= config.MaxConsecutiveLosses)
        {
            rejectionReason = $"Consecutive loss lock active: {consecutiveLosses} consecutive losses recorded (Max: {config.MaxConsecutiveLosses}). Cooldown required.";
            return false;
        }

        // Maximum Simultaneous Positions check
        if (currentOpenPositionsCount >= config.MaxSimultaneousPositions)
        {
            rejectionReason = $"Position limit reached: Already holding {currentOpenPositionsCount} active shadow position(s) (Max: {config.MaxSimultaneousPositions}).";
            return false;
        }

        return true;
    }

    public void CalculatePositionSizing(V2TradeCandidate candidate, QuantitativeConvictionV2Config config, decimal todayRealizedLoss)
    {
        if (candidate.ExpectedEntryPrice <= 0 || candidate.StopLossPrice >= candidate.ExpectedEntryPrice)
        {
            candidate.AllocatedLots = 0;
            candidate.AllocatedQuantity = 0;
            candidate.TotalRiskRupees = 0;
            return;
        }

        // Determine contract lot size: NIFTY 50 = 25 (or current regulatory lot size), SENSEX = 10
        int lotSize = candidate.IndexSymbol.Contains("SENSEX", StringComparison.OrdinalIgnoreCase) ? 10 : 25;

        // Maximum allowable risk for this specific trade
        // E.g., 0.5% of Rs 5,00,000 = Rs 2,500
        decimal maxTradeRisk = config.AccountCapital * (config.RiskPerTradePercent / 100.0m);

        // Cap trade risk if approaching daily loss limit
        var remainingDailyLossBudget = config.MaxDailyLossLimit - todayRealizedLoss;
        if (remainingDailyLossBudget < maxTradeRisk)
        {
            maxTradeRisk = Math.Max(0, remainingDailyLossBudget);
        }

        if (maxTradeRisk <= 0)
        {
            candidate.AllocatedLots = 0;
            candidate.AllocatedQuantity = 0;
            candidate.TotalRiskRupees = 0;
            return;
        }

        // Risk Per Lot Calculation:
        // (Entry - StopLoss) * LotSize + Estimated Transaction Costs
        decimal premiumRiskPerLot = (candidate.ExpectedEntryPrice - candidate.StopLossPrice) * lotSize;
        
        // Estimated transaction costs per lot: Brokerage (Rs 20 buy + Rs 20 sell) + STT/Turnover (~0.06%) + Slippage
        decimal turnoverEstimate = candidate.ExpectedEntryPrice * lotSize;
        decimal estimatedTaxes = turnoverEstimate * (config.ExchangeTaxesPercent / 100.0m);
        decimal slippagePerLot = turnoverEstimate * (config.EstimatedSlippagePercent / 100.0m);
        decimal costPerLot = (config.BrokeragePerOrder * 2) + estimatedTaxes + slippagePerLot;

        candidate.EstimatedCostPerLot = Math.Round(costPerLot, 2);
        decimal totalRiskPerLot = premiumRiskPerLot + costPerLot;

        if (totalRiskPerLot <= 0)
        {
            candidate.AllocatedLots = 0;
            candidate.AllocatedQuantity = 0;
            candidate.TotalRiskRupees = 0;
            return;
        }

        // Lots = Floor(MaxTradeRisk / TotalRiskPerLot)
        int lots = (int)Math.Floor(maxTradeRisk / totalRiskPerLot);

        // Enforce boundary: at least 1 lot if affordable, capped at max shadow size (e.g. 4 lots)
        if (lots < 1 && maxTradeRisk >= (totalRiskPerLot * 0.75m))
        {
            lots = 1; // Allow 1 lot micro-exposure
        }
        lots = Math.Clamp(lots, 0, 4);

        candidate.AllocatedLots = lots;
        candidate.AllocatedQuantity = lots * lotSize;
        candidate.TotalRiskRupees = Math.Round(lots * totalRiskPerLot, 2);
    }
}
