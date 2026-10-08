using AutoFno.Domain.Entities;
using AutoFno.Domain.Models;

namespace AutoFno.Application.Interfaces;

public interface IUpstoxClient
{
    string GetAuthorizationUrl(string state);
    Task<UpstoxConnection> ExchangeCodeForTokenAsync(string code, CancellationToken ct = default);
    Task<UpstoxAuthStatusDto> GetProfileAsync(string accessToken, CancellationToken ct = default);
    Task<List<string>> GetAvailableExpiriesAsync(string? accessToken, string instrumentKey, CancellationToken ct = default);
    Task<OptionChainDto?> GetOptionChainAsync(string? accessToken, string instrumentKey, string expiryDate, CancellationToken ct = default);
    Task<decimal> GetLtpAsync(string? accessToken, string instrumentKey, CancellationToken ct = default);
    Task<(decimal ltp, decimal prevClose, decimal dayChange, decimal dayChangePercent)> GetSpotQuoteAsync(string? accessToken, string instrumentKey, CancellationToken ct = default);
    Task<List<Candle>> GetIntradayCandlesAsync(string instrumentKey, string interval = "1minute", CancellationToken ct = default);
    Task<OrderResultDto> PlaceOrderAsync(string accessToken, PlaceFnoOrderRequest request, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(string accessToken, string orderId, CancellationToken ct = default);
    Task<List<PositionDto>> GetPositionsAsync(string accessToken, CancellationToken ct = default);
    Task<string?> GetRegisteredIpAsync(string accessToken, CancellationToken ct = default);
    Task<string> SetRegisteredIpAsync(string accessToken, string primaryIp, string? secondaryIp = null, CancellationToken ct = default);
}
