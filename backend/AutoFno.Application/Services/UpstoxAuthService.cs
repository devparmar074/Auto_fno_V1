using AutoFno.Application.Exceptions;
using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using AutoFno.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.Services;

public class UpstoxAuthService : IUpstoxAuthService
{
    private readonly IConnectionRepository _connectionRepo;
    private readonly IUpstoxClient _upstoxClient;
    private readonly ILogger<UpstoxAuthService> _logger;

    public UpstoxAuthService(
        IConnectionRepository connectionRepo,
        IUpstoxClient upstoxClient,
        ILogger<UpstoxAuthService> logger)
    {
        _connectionRepo = connectionRepo;
        _upstoxClient = upstoxClient;
        _logger = logger;
    }

    public string GetAuthorizationUrl(string? state = null)
    {
        var s = state ?? Guid.NewGuid().ToString("N");
        return _upstoxClient.GetAuthorizationUrl(s);
    }

    public async Task<UpstoxAuthStatusDto> CompleteOAuthAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new TradingException("Authorization code cannot be empty.");
        }

        var conn = await _upstoxClient.ExchangeCodeForTokenAsync(code, ct);
        var profile = await _upstoxClient.GetProfileAsync(conn.EncryptedAccessToken, ct);

        conn.UserId = profile.UserId;
        conn.UserName = profile.UserName;
        conn.Email = profile.Email;

        await _connectionRepo.SaveConnectionAsync(conn, ct);

        return new UpstoxAuthStatusDto
        {
            IsConnected = true,
            UserId = conn.UserId,
            UserName = conn.UserName,
            Email = conn.Email,
            ExpiresAtUtc = conn.ExpiresAtUtc,
            Broker = conn.Broker
        };
    }

    public async Task<UpstoxAuthStatusDto> SetManualTokenAsync(string accessToken, string? refreshToken = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new TradingException("Access token cannot be empty.");
        }

        var profile = await _upstoxClient.GetProfileAsync(accessToken, ct);

        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
        var nextExpiryIst = nowIst.Date.AddDays(1).AddHours(3).AddMinutes(30); // 03:30 AM IST next day
        var expiresAtUtc = TimeZoneInfo.ConvertTimeToUtc(nextExpiryIst, istZone);

        var conn = new UpstoxConnection
        {
            EncryptedAccessToken = accessToken,
            EncryptedRefreshToken = refreshToken,
            ExpiresAtUtc = expiresAtUtc,
            IsActive = true,
            UserId = profile.UserId,
            UserName = profile.UserName,
            Email = profile.Email,
            Broker = "UPSTOX",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _connectionRepo.SaveConnectionAsync(conn, ct);

        return new UpstoxAuthStatusDto
        {
            IsConnected = true,
            UserId = conn.UserId,
            UserName = conn.UserName,
            Email = conn.Email,
            ExpiresAtUtc = conn.ExpiresAtUtc,
            Broker = conn.Broker
        };
    }

    public async Task<UpstoxAuthStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var conn = await _connectionRepo.GetActiveConnectionAsync(ct);
        if (conn == null || conn.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return new UpstoxAuthStatusDto { IsConnected = false };
        }

        return new UpstoxAuthStatusDto
        {
            IsConnected = true,
            UserId = conn.UserId,
            UserName = conn.UserName,
            Email = conn.Email,
            PrimaryIp = conn.PrimaryIp,
            ExpiresAtUtc = conn.ExpiresAtUtc,
            Broker = conn.Broker
        };
    }

    public async Task<string> GetActiveAccessTokenAsync(CancellationToken ct = default)
    {
        var conn = await _connectionRepo.GetActiveConnectionAsync(ct);
        if (conn == null || conn.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new TradingException("No active or valid Upstox connection found. Please connect your broker.");
        }

        return conn.EncryptedAccessToken;
    }

    public Task<UpstoxConnection?> GetActiveConnectionAsync(CancellationToken ct = default)
    {
        return _connectionRepo.GetActiveConnectionAsync(ct);
    }

    public async Task<string?> GetRegisteredIpAsync(CancellationToken ct = default)
    {
        var token = await GetActiveAccessTokenAsync(ct);
        return await _upstoxClient.GetRegisteredIpAsync(token, ct);
    }

    public async Task<string> SetRegisteredIpAsync(string primaryIp, string? secondaryIp = null, CancellationToken ct = default)
    {
        var token = await GetActiveAccessTokenAsync(ct);
        var result = await _upstoxClient.SetRegisteredIpAsync(token, primaryIp, secondaryIp, ct);

        var conn = await _connectionRepo.GetActiveConnectionAsync(ct);
        if (conn != null)
        {
            conn.PrimaryIp = primaryIp;
            await _connectionRepo.SaveConnectionAsync(conn, ct);
        }

        return result;
    }
}
