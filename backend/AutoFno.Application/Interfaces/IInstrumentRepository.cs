using AutoFno.Domain.Entities;

namespace AutoFno.Application.Interfaces;

public interface IInstrumentRepository
{
    Task<Instrument?> GetByKeyAsync(string instrumentKey, CancellationToken ct = default);
    Task<Instrument?> GetBySymbolAsync(string tradingSymbol, CancellationToken ct = default);
    Task<List<Instrument>> GetByUnderlyingAsync(string underlyingSymbol, CancellationToken ct = default);
    Task UpsertAsync(Instrument instrument, CancellationToken ct = default);
    Task BulkUpsertAsync(IEnumerable<Instrument> instruments, CancellationToken ct = default);
}
