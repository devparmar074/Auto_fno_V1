using AutoFno.Application.Interfaces;
using AutoFno.Domain.Models;
using AutoFno.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AutoFno.Infrastructure.Services;

public class SignalRTradingNotificationService : ITradingNotificationService
{
    private readonly IHubContext<TradingHub> _hubContext;
    private readonly ILogger<SignalRTradingNotificationService> _logger;

    public SignalRTradingNotificationService(
        IHubContext<TradingHub> hubContext,
        ILogger<SignalRTradingNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastSpotQuoteAsync(string indexSymbol, decimal ltp, decimal dayChange, decimal dayChangePercent)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveSpotQuote", new
        {
            IndexSymbol = indexSymbol,
            Ltp = ltp,
            DayChange = dayChange,
            DayChangePercent = dayChangePercent,
            TimestampUtc = DateTime.UtcNow
        });
    }

    public async Task BroadcastOptionChainAsync(OptionChainDto optionChain)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveOptionChain", optionChain);
    }

    public async Task BroadcastPcrMetricsAsync(PcrSummaryDto pcr, MaxPainDto maxPain)
    {
        await _hubContext.Clients.All.SendAsync("ReceivePcrMetrics", new
        {
            Pcr = pcr,
            MaxPain = maxPain
        });
    }

    public async Task BroadcastStrategyScoreAsync(FnOStrategyScoreDto score)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveStrategyScore", score);
    }

    public async Task BroadcastPositionsAsync(List<PositionDto> positions, decimal totalUnrealizedPnl, decimal todayRealizedPnl)
    {
        await _hubContext.Clients.All.SendAsync("ReceivePositions", new
        {
            Positions = positions,
            TotalUnrealizedPnl = totalUnrealizedPnl,
            TodayRealizedPnl = todayRealizedPnl,
            TimestampUtc = DateTime.UtcNow
        });
    }

    public async Task BroadcastSemiAutoAlertAsync(FnOStrategyScoreDto alert)
    {
        _logger.LogInformation("Broadcasting SemiAuto execution alert: {Rec} for {Strike}", alert.Recommendation, alert.RecommendedStrike);
        await _hubContext.Clients.All.SendAsync("ReceiveSemiAutoAlert", alert);
    }

    public async Task BroadcastOrderUpdateAsync(OrderResultDto orderResult)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveOrderUpdate", orderResult);
    }
}
