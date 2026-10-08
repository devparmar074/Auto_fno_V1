using AutoFno.Domain.Models;

namespace AutoFno.Application.Interfaces;

public interface ITradingNotificationService
{
    Task BroadcastSpotQuoteAsync(string indexSymbol, decimal ltp, decimal dayChange, decimal dayChangePercent);
    Task BroadcastOptionChainAsync(OptionChainDto optionChain);
    Task BroadcastPcrMetricsAsync(PcrSummaryDto pcr, MaxPainDto maxPain);
    Task BroadcastStrategyScoreAsync(FnOStrategyScoreDto score);
    Task BroadcastPositionsAsync(List<PositionDto> positions, decimal totalUnrealizedPnl, decimal todayRealizedPnl);
    Task BroadcastSemiAutoAlertAsync(FnOStrategyScoreDto alert);
    Task BroadcastOrderUpdateAsync(OrderResultDto orderResult);
}
