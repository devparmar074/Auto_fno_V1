using System.Data;
using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using Dapper;

namespace AutoFno.Infrastructure.Data.Repositories;

public class InstrumentRepository : IInstrumentRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public InstrumentRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Instrument?> GetByKeyAsync(string instrumentKey, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = "SELECT TOP 1 * FROM dbo.Instrument WHERE InstrumentKey = @InstrumentKey;";
        return await db.QuerySingleOrDefaultAsync<Instrument>(sql, new { InstrumentKey = instrumentKey });
    }

    public async Task<Instrument?> GetBySymbolAsync(string tradingSymbol, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = "SELECT TOP 1 * FROM dbo.Instrument WHERE TradingSymbol = @TradingSymbol;";
        return await db.QuerySingleOrDefaultAsync<Instrument>(sql, new { TradingSymbol = tradingSymbol });
    }

    public async Task<List<Instrument>> GetByUnderlyingAsync(string underlyingSymbol, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM dbo.Instrument WHERE UnderlyingSymbol = @UnderlyingSymbol ORDER BY StrikePrice ASC;";
        var results = await db.QueryAsync<Instrument>(sql, new { UnderlyingSymbol = underlyingSymbol });
        return results.ToList();
    }

    public async Task UpsertAsync(Instrument instrument, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        const string sql = @"
            MERGE INTO dbo.Instrument AS Target
            USING (SELECT @InstrumentKey AS InstrumentKey) AS Source
            ON Target.InstrumentKey = Source.InstrumentKey
            WHEN MATCHED THEN
                UPDATE SET TradingSymbol = @TradingSymbol,
                           CompanyName = @CompanyName,
                           Exchange = @Exchange,
                           Segment = @Segment,
                           UnderlyingKey = @UnderlyingKey,
                           UnderlyingSymbol = @UnderlyingSymbol,
                           StrikePrice = @StrikePrice,
                           OptionType = @OptionType,
                           ExpiryDate = @ExpiryDate,
                           LotSize = @LotSize,
                           TickSize = @TickSize,
                           FreezeQuantity = @FreezeQuantity,
                           ExchangeToken = @ExchangeToken,
                           LastUpdatedUtc = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (InstrumentKey, TradingSymbol, CompanyName, Exchange, Segment, UnderlyingKey, UnderlyingSymbol, StrikePrice, OptionType, ExpiryDate, LotSize, TickSize, FreezeQuantity, ExchangeToken, LastUpdatedUtc)
                VALUES (@InstrumentKey, @TradingSymbol, @CompanyName, @Exchange, @Segment, @UnderlyingKey, @UnderlyingSymbol, @StrikePrice, @OptionType, @ExpiryDate, @LotSize, @TickSize, @FreezeQuantity, @ExchangeToken, SYSUTCDATETIME());";

        await db.ExecuteAsync(sql, instrument);
    }

    public async Task BulkUpsertAsync(IEnumerable<Instrument> instruments, CancellationToken ct = default)
    {
        foreach (var inst in instruments)
        {
            await UpsertAsync(inst, ct);
        }
    }
}
