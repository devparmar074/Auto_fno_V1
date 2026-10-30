using System;
using System.Collections.Generic;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;

namespace AutoFno.Application.V2.Engines;

public class TradeQualificationEngine : ITradeQualificationEngine
{
    private readonly IRiskManagementEngineV2 _riskEngine;

    public TradeQualificationEngine(IRiskManagementEngineV2 riskEngine)
    {
        _riskEngine = riskEngine;
    }

    public V2TradeCandidate QualifyTrade(
        V2ConvictionScoreDto score,
        OptionChainDto optionChain,
        List<Candle> spotCandles,
        QuantitativeConvictionV2Config config,
        int currentOpenPositionsCount,
        decimal todayRealizedPnl,
        int consecutiveLosses = 0)
    {
        var candidate = new V2TradeCandidate
        {
            IndexSymbol = score.IndexSymbol,
            SpotPrice = score.SpotPrice,
            TimestampUtc = DateTime.UtcNow,
            ConvictionScore = score.TotalScore,
            Decision = score.CandidateDecision
        };

        // 1. Initial Candidate Direction Check
        if (score.CandidateDecision == TradeCandidateDecision.NoTrade || Math.Abs(score.TotalScore) < config.BullishCandidateThreshold)
        {
            candidate.IsQualified = false;
            candidate.RejectionReasons |= QualificationRejectionReason.ScoreBelowThreshold;
            candidate.RejectionDetails.Add($"Score {score.TotalScore:F1} is below minimum conviction threshold ({config.BullishCandidateThreshold:F0}).");
            return candidate;
        }

        var isBullish = score.CandidateDecision == TradeCandidateDecision.BuyAtmCeCandidate;
        candidate.OptionType = isBullish ? OptionType.CE : OptionType.PE;

        // 2. Signal Agreement Filter
        // For Bullish: PCR and Trend scores should not be negative
        // For Bearish: PCR and Trend scores should not be positive
        if (isBullish && (score.PcrAnalysis.TotalPcrPillarScore < -3.0m || score.SpotTrend.TotalSpotPillarScore < -3.0m))
        {
            candidate.RejectionReasons |= QualificationRejectionReason.ConflictingSignals;
            candidate.RejectionDetails.Add($"Conflicting signals: PCR ({score.PcrAnalysis.TotalPcrPillarScore:F1}) or Spot Trend ({score.SpotTrend.TotalSpotPillarScore:F1}) opposes bullish candidate.");
        }
        else if (!isBullish && (score.PcrAnalysis.TotalPcrPillarScore > 3.0m || score.SpotTrend.TotalSpotPillarScore > 3.0m))
        {
            candidate.RejectionReasons |= QualificationRejectionReason.ConflictingSignals;
            candidate.RejectionDetails.Add($"Conflicting signals: PCR ({score.PcrAnalysis.TotalPcrPillarScore:F1}) or Spot Trend ({score.SpotTrend.TotalSpotPillarScore:F1}) opposes bearish candidate.");
        }

        // 3. Spot Trend Confirmation Filter
        if (isBullish && !score.SpotTrend.IsSupertrendBullish && score.SpotPrice < score.SpotTrend.Ema21)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.SpotTrendMismatch;
            candidate.RejectionDetails.Add("Spot trend mismatch: Supertrend is bearish and spot is below EMA 21.");
        }
        else if (!isBullish && score.SpotTrend.IsSupertrendBullish && score.SpotPrice > score.SpotTrend.Ema21)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.SpotTrendMismatch;
            candidate.RejectionDetails.Add("Spot trend mismatch: Supertrend is bullish and spot is above EMA 21.");
        }

        // 4. Underlying Price Structure & Overextension Filter
        if (score.SpotTrend.IsOverextended)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.UnderlyingPriceStructureUnfavorable;
            candidate.RejectionDetails.Add("Underlying price structure overextended: Price is > 2.5 ATRs away from EMA 9.");
        }

        // Check if buying into immediate major resistance (Call Wall) or selling into immediate major support (Put Wall)
        if (isBullish && score.OiAnalysis.CallWallStrike > 0 && Math.Abs(score.SpotPrice - score.OiAnalysis.CallWallStrike) < 15m)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.UnderlyingPriceStructureUnfavorable;
            candidate.RejectionDetails.Add($"Bullish entry directly into Call Wall resistance ({score.OiAnalysis.CallWallStrike:F0}). High rejection risk.");
        }
        else if (!isBullish && score.OiAnalysis.PutWallStrike > 0 && Math.Abs(score.SpotPrice - score.OiAnalysis.PutWallStrike) < 15m)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.UnderlyingPriceStructureUnfavorable;
            candidate.RejectionDetails.Add($"Bearish entry directly into Put Wall support ({score.OiAnalysis.PutWallStrike:F0}). High bounce risk.");
        }

        // Locate Target Option Contract (ATM strike)
        var atmStrikeObj = optionChain.Strikes?.FirstOrDefault(s => s.IsAtm)
            ?? optionChain.Strikes?.OrderBy(s => Math.Abs(s.StrikePrice - score.SpotPrice)).FirstOrDefault();

        if (atmStrikeObj == null)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.StaleOrMissingMarketData;
            candidate.RejectionDetails.Add("Could not resolve valid ATM option strike contract.");
            candidate.IsQualified = false;
            return candidate;
        }

        candidate.TargetStrike = atmStrikeObj.StrikePrice;
        var contract = isBullish ? atmStrikeObj.Call : atmStrikeObj.Put;

        if (contract == null || contract.Ltp <= 0)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.StaleOrMissingMarketData;
            candidate.RejectionDetails.Add($"Target option contract {atmStrikeObj.StrikePrice:F0} {(isBullish ? "CE" : "PE")} has zero or missing LTP.");
            candidate.IsQualified = false;
            return candidate;
        }

        candidate.InstrumentKey = contract.InstrumentKey;
        candidate.TradingSymbol = contract.TradingSymbol;
        candidate.ExpectedEntryPrice = contract.Ltp;

        // 5. Option Liquidity Filter
        if (contract.OpenInterest < config.MinOptionOpenInterest)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.OptionLiquidityInsufficient;
            candidate.RejectionDetails.Add($"Option Open Interest ({contract.OpenInterest:N0}) is below minimum threshold ({config.MinOptionOpenInterest:N0}).");
        }
        if (contract.Volume < config.MinOptionVolume)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.OptionLiquidityInsufficient;
            candidate.RejectionDetails.Add($"Option Volume ({contract.Volume:N0}) is below minimum threshold ({config.MinOptionVolume:N0}).");
        }

        // 6. Bid/Ask Spread Filter
        if (contract.AskPrice > 0 && contract.BidPrice > 0 && contract.Ltp > 0)
        {
            var spreadPercent = ((contract.AskPrice - contract.BidPrice) / contract.Ltp) * 100m;
            if (spreadPercent > config.MaxBidAskSpreadPercent)
            {
                candidate.RejectionReasons |= QualificationRejectionReason.BidAskSpreadTooWide;
                candidate.RejectionDetails.Add($"Option Bid/Ask spread ({spreadPercent:F2}%) exceeds maximum limit ({config.MaxBidAskSpreadPercent:F2}%).");
            }
        }

        // 7. IV Regime Filter
        if (score.GreeksAnalysis.IvEnvironment == IvEnvironment.CrushRisk)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.UnfavorableIVRegime;
            candidate.RejectionDetails.Add($"Option IV ({score.GreeksAnalysis.AtmIv:F1}%) is in extreme volatility crush risk territory. Long options forbidden.");
        }

        // 8. Expected Premium Movement Filter
        var expectedMove = isBullish ? score.GreeksAnalysis.EstimatedPremiumChangeCe : score.GreeksAnalysis.EstimatedPremiumChangePe;
        candidate.ExpectedMovePremium = expectedMove;
        if (expectedMove < config.MinExpectedPremiumMovement)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.InsufficientExpectedPremiumMovement;
            candidate.RejectionDetails.Add($"Estimated premium movement (+Rs {expectedMove:F1}) is insufficient to justify directional trade (Min required: Rs {config.MinExpectedPremiumMovement:F1}).");
        }

        // Calculate Stops, Targets & Risk/Reward
        // 25% Stop Loss on premium, 40% Target 1, 75% Target 2
        candidate.StopLossPrice = Math.Round(candidate.ExpectedEntryPrice * 0.75m, 2);
        candidate.Target1Price = Math.Round(candidate.ExpectedEntryPrice * 1.40m, 2);
        candidate.Target2Price = Math.Round(candidate.ExpectedEntryPrice * 1.75m, 2);

        var riskPerShare = candidate.ExpectedEntryPrice - candidate.StopLossPrice;
        var rewardPerShare = candidate.Target1Price - candidate.ExpectedEntryPrice;
        candidate.RiskRewardRatio = riskPerShare > 0 ? Math.Round(rewardPerShare / riskPerShare, 2) : 0m;

        // 9. Risk / Reward Filter
        if (candidate.RiskRewardRatio < config.MinRiskRewardRatio)
        {
            candidate.RejectionReasons |= QualificationRejectionReason.UnfavorableRiskReward;
            candidate.RejectionDetails.Add($"Risk/Reward ratio ({candidate.RiskRewardRatio:F2}) is below required minimum ({config.MinRiskRewardRatio:F2}).");
        }

        // 10. Risk Management Engine Validation & Position Sizing
        if (!_riskEngine.IsRiskPermitted(candidate, config, currentOpenPositionsCount, todayRealizedPnl, consecutiveLosses, out var riskRejection))
        {
            candidate.RejectionReasons |= QualificationRejectionReason.DailyLossLimitReached;
            candidate.RejectionDetails.Add(riskRejection);
        }
        else
        {
            _riskEngine.CalculatePositionSizing(candidate, config, todayRealizedPnl < 0 ? Math.Abs(todayRealizedPnl) : 0m);
        }

        // Qualification Result
        candidate.IsQualified = (candidate.RejectionReasons == QualificationRejectionReason.None) && (candidate.AllocatedLots > 0);

        if (!candidate.IsQualified && candidate.Decision != TradeCandidateDecision.NoTrade)
        {
            // Output remains structured, but qualification fails
            candidate.Decision = TradeCandidateDecision.NoTrade;
        }

        return candidate;
    }
}
