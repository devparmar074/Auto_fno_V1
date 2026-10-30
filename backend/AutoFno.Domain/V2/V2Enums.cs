using System;

namespace AutoFno.Domain.V2;

public enum TradeCandidateDecision
{
    NoTrade = 0,
    BuyAtmCeCandidate = 1,
    BuyAtmPeCandidate = 2
}

public enum MarketRegime
{
    RangeboundChoppy = 0,
    StrongBullTrend = 1,
    MildBullBias = 2,
    MildBearBias = 3,
    StrongBearTrend = 4,
    HighVolatilityBreakout = 5
}

[Flags]
public enum QualificationRejectionReason
{
    None = 0,
    ScoreBelowThreshold = 1 << 0,
    ConflictingSignals = 1 << 1,
    SpotTrendMismatch = 1 << 2,
    UnderlyingPriceStructureUnfavorable = 1 << 3,
    OptionLiquidityInsufficient = 1 << 4,
    BidAskSpreadTooWide = 1 << 5,
    StaleOrMissingMarketData = 1 << 6,
    UnfavorableIVRegime = 1 << 7,
    InsufficientExpectedPremiumMovement = 1 << 8,
    UnfavorableRiskReward = 1 << 9,
    MaxOpenPositionsReached = 1 << 10,
    SessionRestricted = 1 << 11,
    DailyLossLimitReached = 1 << 12,
    CooldownActive = 1 << 13
}

public enum V2OperatingMode
{
    V1Only = 0,
    V2ShadowMode = 1,
    V2LiveMode = 2
}

public enum V2ExitReason
{
    None = 0,
    Target1Hit = 1,
    Target2Hit = 2,
    TrailingStopHit = 3,
    UnderlyingInvalidation = 4,
    TimeCutoffExit = 5,
    EmergencyLossLock = 6
}

public enum IvEnvironment
{
    StableNormal = 0,
    LowCompressed = 1,
    ElevatedHigh = 2,
    CrushRisk = 3
}
