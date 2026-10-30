using System;

namespace AutoFno.Domain.V2;

public class FrictionModelConfig
{
    public decimal BrokeragePerTrade { get; set; } = 40.0m;
    public decimal ExchangeTurnoverRate { get; set; } = 0.00053m;
    public decimal SttRate { get; set; } = 0.001m;
    public decimal GstRate { get; set; } = 0.18m;
    public decimal StampDutyRate { get; set; } = 0.00003m;
    public decimal SlippagePercent { get; set; } = 0.25m;
}

public class QuantitativeConvictionV2Config
{
    // Feature Flags - Strictly false by default
    public bool EnableV2 { get; set; } = false;
    public bool V2ShadowMode { get; set; } = false;
    public bool V2LiveTrading { get; set; } = false; // Must NEVER default to true

    // Operating Mode
    public V2OperatingMode OperatingMode => 
        V2LiveTrading ? V2OperatingMode.V2LiveMode :
        V2ShadowMode ? V2OperatingMode.V2ShadowMode :
        V2OperatingMode.V1Only;

    // Thresholds
    public decimal BullishCandidateThreshold { get; set; } = 60.0m;
    public decimal BearishCandidateThreshold { get; set; } = -60.0m;

    // Pillar 1: PCR Sentiment (25 pts)
    public decimal VolumePcrFlowWeight { get; set; } = 10.0m;
    public decimal AtmPcrWeight { get; set; } = 7.0m;
    public decimal PcrSlopeVelocityWeight { get; set; } = 5.0m;
    public decimal BroaderOiPcrWeight { get; set; } = 3.0m;

    // Pillar 2: OI Distribution & Max Pain (25 pts)
    public decimal BuildupDominanceWeight { get; set; } = 10.0m;
    public decimal FreshWritingFlowWeight { get; set; } = 8.0m;
    public decimal OiSupportResistanceWeight { get; set; } = 5.0m;
    public decimal MaxPainDistanceWeight { get; set; } = 2.0m;

    // Pillar 3: Option Greeks & IV Regime (25 pts)
    public decimal AtmDeltaAsymmetryWeight { get; set; } = 6.0m;
    public decimal IvRegimeMomentumWeight { get; set; } = 7.0m;
    public decimal CallPutIvSkewWeight { get; set; } = 5.0m;
    public decimal GammaThetaEfficiencyWeight { get; set; } = 7.0m;

    // Pillar 4: Spot Momentum & Trend (25 pts)
    public decimal EmaAlignmentSlopeWeight { get; set; } = 8.0m;
    public decimal SupertrendDirectionWeight { get; set; } = 5.0m;
    public decimal RsiMomentumRegimeWeight { get; set; } = 4.0m;
    public decimal VwapPriceStructureWeight { get; set; } = 8.0m;

    // Qualification Filters
    public long MinOptionOpenInterest { get; set; } = 50000;
    public long MinOptionVolume { get; set; } = 25000;
    public decimal MaxBidAskSpreadPercent { get; set; } = 1.5m; // Max 1.5% spread
    public int MaxDataAgeSeconds { get; set; } = 60;
    public decimal MinRiskRewardRatio { get; set; } = 1.5m;
    public decimal MinExpectedPremiumMovement { get; set; } = 5.0m; // Rs points
    public int SignalPersistenceSeconds { get; set; } = 10;
    public int CooldownMinutesAfterLoss { get; set; } = 15;
    public int MinMinutesAfterMarketOpen { get; set; } = 15;
    public int MinMinutesBeforeMarketClose { get; set; } = 15;

    // Risk Management Parameters (Hypothetical Shadow Engine)
    public decimal AccountCapital { get; set; } = 500000.0m; // Rs 5,00,000 baseline
    public decimal RiskPerTradePercent { get; set; } = 0.5m; // 0.5% risk = Rs 2,500
    public decimal MaxDailyLossLimit { get; set; } = 5000.0m; // 2R daily loss lock
    public int MaxConsecutiveLosses { get; set; } = 3;
    public int MaxSimultaneousPositions { get; set; } = 1;
    public int MaxDailyTrades { get; set; } = 5;
    public decimal EstimatedSlippagePercent { get; set; } = 0.25m;
    public decimal BrokeragePerOrder { get; set; } = 20.0m;
    public decimal ExchangeTaxesPercent { get; set; } = 0.06m;

    public FrictionModelConfig FrictionModel { get; set; } = new();

    // Trade Management Parameters
    public decimal PartialProfitBookingRMultiple { get; set; } = 1.0m;
    public decimal TrailingStopActivationRMultiple { get; set; } = 1.5m;
    public decimal FullTargetRMultiple { get; set; } = 2.5m;
}
