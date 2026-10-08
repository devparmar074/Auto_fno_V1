using AutoFno.Domain.Models;

namespace AutoFno.Application.Interfaces;

public interface IPortfolioService
{
    Task<List<PositionDto>> GetPositionsAsync(CancellationToken ct = default);
    Task<(decimal TotalUnrealizedPnl, decimal TodayRealizedPnl)> GetPnlSummaryAsync(CancellationToken ct = default);
}
