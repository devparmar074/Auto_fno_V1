using System.Data;
using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using Dapper;

namespace AutoFno.Infrastructure.Data.Repositories;

public class FnOStrategyRepository : IFnOStrategyRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public FnOStrategyRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BotConfig> GetBotConfigAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var config = await db.QuerySingleOrDefaultAsync<BotConfig>(
            "dbo.sp_GetBotConfig",
            commandType: CommandType.StoredProcedure);

        return config ?? new BotConfig();
    }

    public async Task<BotConfig> UpdateBotConfigAsync(BotConfig config, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var updated = await db.QuerySingleOrDefaultAsync<BotConfig>(
            "dbo.sp_UpdateBotConfig",
            new
            {
                Mode = (int)config.Mode,
                config.ActiveIndex,
                config.DefaultLots,
                config.MaxDailyLossLimit,
                config.MaxDailyProfitTarget,
                config.StopLossPercent,
                config.TakeProfitPercent,
                config.MaxOpenPositions,
                config.IsKillSwitchActive,
                config.AutoSquareOffAtCutoff,
                AutoSquareOffTimeIst = config.AutoSquareOffTimeIst
            },
            commandType: CommandType.StoredProcedure);

        return updated ?? config;
    }

    public async Task<long> SaveSignalAsync(StrategySignalHistory signal, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var id = await db.ExecuteScalarAsync<long>(
            "dbo.sp_SaveStrategySignal",
            new
            {
                signal.IndexSymbol,
                signal.SpotPrice,
                signal.TotalScore,
                signal.PcrScore,
                signal.OiMaxPainScore,
                signal.GreeksIvScore,
                signal.SpotTrendScore,
                Recommendation = (int)signal.Recommendation,
                signal.RecommendedStrike,
                RecommendedOptionType = (int?)signal.RecommendedOptionType,
                signal.SetupRationale
            },
            commandType: CommandType.StoredProcedure);

        signal.Id = id;
        return id;
    }

    public async Task<List<StrategySignalHistory>> GetRecentSignalsAsync(string? indexSymbol = null, int top = 30, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var results = await db.QueryAsync<StrategySignalHistory>(
            "dbo.sp_GetRecentStrategySignals",
            new { IndexSymbol = indexSymbol, Top = top },
            commandType: CommandType.StoredProcedure);
        return results.ToList();
    }
}
