using AutoFno.Domain.Models;

namespace AutoFno.Application.Interfaces;

public interface IFnOMarketDataService
{
    string ActiveIndex { get; set; }
    string? SelectedExpiry { get; set; }

    Task<OptionChainDto> GetOptionChainAsync(string? indexSymbol = null, string? expiryDate = null, CancellationToken ct = default);
    Task<List<string>> GetAvailableExpiriesAsync(string? indexSymbol = null, CancellationToken ct = default);
    Task<PcrSummaryDto> GetPcrSummaryAsync(string? indexSymbol = null, CancellationToken ct = default);
    Task<MaxPainDto> GetMaxPainAsync(string? indexSymbol = null, CancellationToken ct = default);
    Task<decimal> GetSpotLtpAsync(string? indexSymbol = null, CancellationToken ct = default);
    Task<List<Candle>> GetSpotCandlesAsync(string? indexSymbol = null, string interval = "1minute", CancellationToken ct = default);
    Task<DashboardFnoSummaryDto> GetDashboardSummaryAsync(string? indexSymbol = null, CancellationToken ct = default);
    bool IsMarketOpen();
}
