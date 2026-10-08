using AutoFno.Application.Interfaces;
using AutoFno.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly ITradeRepository _tradeRepo;
    private readonly IUpstoxClient _upstoxClient;
    private readonly IUpstoxAuthService _authService;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(
        ITradeRepository tradeRepo,
        IUpstoxClient upstoxClient,
        IUpstoxAuthService authService,
        ILogger<PortfolioService> logger)
    {
        _tradeRepo = tradeRepo;
        _upstoxClient = upstoxClient;
        _authService = authService;
        _logger = logger;
    }

    public async Task<List<PositionDto>> GetPositionsAsync(CancellationToken ct = default)
    {
        var auth = await _authService.GetStatusAsync(ct);
        if (auth.IsConnected)
        {
            try
            {
                var token = await _authService.GetActiveAccessTokenAsync(ct);
                var brokerPositions = await _upstoxClient.GetPositionsAsync(token, ct);
                if (brokerPositions.Count > 0)
                {
                    return brokerPositions;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed fetching broker positions: {Message}. Falling back to local database.", ex.Message);
            }
        }

        // Fallback to local position snapshots
        var snapshots = await _tradeRepo.GetLatestPositionsAsync(ct);
        return snapshots.Select(s => new PositionDto
        {
            InstrumentKey = s.InstrumentKey,
            TradingSymbol = s.TradingSymbol,
            IndexSymbol = s.IndexSymbol,
            StrikePrice = s.StrikePrice,
            OptionType = s.OptionType,
            Quantity = s.Quantity,
            Lots = s.Lots,
            AveragePrice = s.AveragePrice,
            CurrentLtp = s.CurrentLtp,
            UnrealizedPnl = s.UnrealizedPnl,
            RealizedPnl = s.RealizedPnl
        }).ToList();
    }

    public async Task<(decimal TotalUnrealizedPnl, decimal TodayRealizedPnl)> GetPnlSummaryAsync(CancellationToken ct = default)
    {
        var positions = await GetPositionsAsync(ct);
        decimal unrealized = positions.Sum(p => p.UnrealizedPnl);
        decimal realized = positions.Sum(p => p.RealizedPnl);

        return (unrealized, realized);
    }
}
