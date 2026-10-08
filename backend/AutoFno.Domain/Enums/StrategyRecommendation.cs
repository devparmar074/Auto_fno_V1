namespace AutoFno.Domain.Enums;

public enum StrategyRecommendation
{
    Neutral = 0,
    StrongBuyCall = 1,     // High conviction bullish -> Buy CE
    StrongBuyPut = 2,      // High conviction bearish -> Buy PE
    StrongSellCall = 3,    // Bearish / Resistance rejection -> Sell CE
    StrongSellPut = 4,     // Bullish / Support bounce -> Sell PE
    RangeboundHold = 5,    // Theta decay zone / Neutral -> Hold / Iron Condor
    ExitAll = 6            // Risk threshold breached or Market close cutoff
}
