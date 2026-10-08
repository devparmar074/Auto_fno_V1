using AutoFno.Domain.Entities;
using AutoFno.Domain.Models;

namespace AutoFno.Application.Interfaces;

public interface IUpstoxAuthService
{
    string GetAuthorizationUrl(string? state = null);
    Task<UpstoxAuthStatusDto> CompleteOAuthAsync(string code, CancellationToken ct = default);
    Task<UpstoxAuthStatusDto> SetManualTokenAsync(string accessToken, string? refreshToken = null, CancellationToken ct = default);
    Task<UpstoxAuthStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<string> GetActiveAccessTokenAsync(CancellationToken ct = default);
    Task<UpstoxConnection?> GetActiveConnectionAsync(CancellationToken ct = default);
    Task<string?> GetRegisteredIpAsync(CancellationToken ct = default);
    Task<string> SetRegisteredIpAsync(string primaryIp, string? secondaryIp = null, CancellationToken ct = default);
}
