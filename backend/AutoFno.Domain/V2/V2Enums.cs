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

public enum MaturityLevel
{
    Level0_ConceptualArchitecture = 0,
    Level1_MathematicalSpecification = 1,
    Level2_EngineImplementation = 2,
    Level3_UnitAndIntegrationTesting = 3,
    Level4_HistoricalBacktesting = 4,
    Level5_ShadowPaperTrading = 5,
    Level6_ForwardValidation = 6,
    Level7_GuardedLiveTrading = 7,
    Level8_FullyAutomatedLiveTrading = 8,
    Level9_SelfOptimizingSystem = 9
}

public enum ValidationCampaignStatus
{
    Configured = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Failed = 4
}

public enum GuardedOrderStatus
{
    PendingApproval = 0,
    Approved = 1,
    RejectedByUser = 2,
    TimedOut = 3,
    OrderDispatched = 4,
    Acknowledged = 5,
    PartiallyFilled = 6,
    Filled = 7,
    RejectedByBroker = 8,
    Cancelled = 9,
    EmergencyFlattened = 10
}

public enum AutomatedExecutionState
{
    Standby = 0,
    Scanning = 1,
    OrderPlaced = 2,
    PositionOpen = 3,
    Target1Closed = 4,
    TrailingStopActive = 5,
    PositionClosed = 6,
    CircuitBreakerTripped = 7,
    EmergencyHalt = 8
}

public enum MLModelStatus
{
    Untrained = 0,
    Trained = 1,
    Candidate = 2,
    ShadowTesting = 3,
    Approved = 4,
    Active = 5,
    RolledBack = 6,
    Retired = 7
}

public enum OptimizationObjective
{
    Expectancy = 0,
    ProfitFactor = 1,
    RiskAdjustedSharpe = 2,
    CalmarRatio = 3
}

public enum SystemHealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Critical = 2,
    EmergencyStop = 3
}

public enum BacktestExecutionSlippageModel
{
    ConservativeBidAsk = 0,
    FixedPercentage = 1,
    VolatilityAdjusted = 2
}
