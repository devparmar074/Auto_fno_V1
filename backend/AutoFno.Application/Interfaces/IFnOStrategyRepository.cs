using AutoFno.Domain.Entities;

namespace AutoFno.Application.Interfaces;

public interface IFnOStrategyRepository
{
    Task<BotConfig> GetBotConfigAsync(CancellationToken ct = default);
    Task<BotConfig> UpdateBotConfigAsync(BotConfig config, CancellationToken ct = default);
    Task<long> SaveSignalAsync(StrategySignalHistory signal, CancellationToken ct = default);
    Task<List<StrategySignalHistory>> GetRecentSignalsAsync(string? indexSymbol = null, int top = 30, CancellationToken ct = default);
}
