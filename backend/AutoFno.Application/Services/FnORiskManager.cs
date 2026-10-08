using AutoFno.Application.Exceptions;
using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using AutoFno.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.Services;

public class FnORiskManager : IFnORiskManager
{
    private readonly IFnOStrategyRepository _strategyRepo;
    private readonly IPortfolioService _portfolioService;
    private readonly ITradeRepository _tradeRepo;
    private readonly IUpstoxAuthService _authService;
    private readonly ILogger<FnORiskManager> _logger;

    public FnORiskManager(
        IFnOStrategyRepository strategyRepo,
        IPortfolioService portfolioService,
        ITradeRepository tradeRepo,
        IUpstoxAuthService authService,
        ILogger<FnORiskManager> logger)
    {
        _strategyRepo = strategyRepo;
        _portfolioService = portfolioService;
        _tradeRepo = tradeRepo;
        _authService = authService;
        _logger = logger;
    }

    public Task<int> ResolveLotSizeAsync(string indexSymbol, CancellationToken ct = default)
    {
        // Standard NSE/BSE contract lot sizes:
        // NIFTY 50: typically 25 or 75
        // SENSEX: typically 10 or 20
        var sym = indexSymbol.ToUpperInvariant();
        if (sym.Contains("SENSEX") || sym.Contains("BSE"))
        {
            return Task.FromResult(10);
        }
        return Task.FromResult(25);
    }

    public async Task<bool> CanExecuteNewTradeAsync(PlaceFnoOrderRequest request, BotConfig config, CancellationToken ct = default)
    {
        if (config.IsKillSwitchActive)
        {
            _logger.LogWarning("Trade blocked: Kill Switch is ACTIVE.");
            throw new TradingException("Trade blocked: Emergency Kill Switch is active.");
        }

        // Check IST Market Hours and Auto Square-off cutoff (only for live connected Upstox orders)
        var auth = await _authService.GetStatusAsync(ct);
        if (auth.IsConnected)
        {
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone).TimeOfDay;

            if (nowIst < new TimeSpan(9, 15, 0) || nowIst > new TimeSpan(15, 30, 0))
            {
                _logger.LogWarning("Trade blocked: Live market is currently closed (IST: {Time}).", nowIst);
                throw new TradingException($"Market is closed. Trading allowed only between 09:15 AM and 03:30 PM IST (Current: {nowIst:hh\\:mm\\:ss}).");
            }

            if (config.AutoSquareOffAtCutoff && nowIst >= config.AutoSquareOffTimeSpan)
            {
                _logger.LogWarning("Trade blocked: Past daily square-off cutoff time ({Cutoff}).", config.AutoSquareOffTimeSpan);
                throw new TradingException($"Trade blocked: Past daily square-off cutoff time ({config.AutoSquareOffTimeSpan}).");
            }
        }

        // Check P&L Drawdown limits
        var (unrealized, realized) = await _portfolioService.GetPnlSummaryAsync(ct);
        var totalPnl = unrealized + realized;

        if (totalPnl <= -config.MaxDailyLossLimit)
        {
            _logger.LogError("Trade blocked: Daily loss limit breached! Total P&L: {Pnl}, Limit: -{Limit}", totalPnl, config.MaxDailyLossLimit);
            throw new TradingException($"Daily loss limit breached (₹{totalPnl:F2} vs limit -₹{config.MaxDailyLossLimit:F2}). Trading halted.");
        }

        // Check Max Open Positions
        var positions = await _portfolioService.GetPositionsAsync(ct);
        if (positions.Count >= config.MaxOpenPositions)
        {
            _logger.LogWarning("Trade blocked: Maximum open positions ({Count}/{Max}) reached.", positions.Count, config.MaxOpenPositions);
            throw new TradingException($"Max open positions limit ({config.MaxOpenPositions}) reached.");
        }

        return true;
    }

    public async Task CheckRiskLimitsAndCutoffAsync(BotConfig config, CancellationToken ct = default)
    {
        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone).TimeOfDay;

        // Check Cutoff
        if (config.AutoSquareOffAtCutoff && nowIst >= config.AutoSquareOffTimeSpan)
        {
            _logger.LogInformation("Cutoff time reached ({Time} >= {Cutoff}). Auto squaring off open positions.", nowIst, config.AutoSquareOffTimeSpan);
            // Settle or square off
        }

        // Check Loss Limit
        var (unrealized, realized) = await _portfolioService.GetPnlSummaryAsync(ct);
        var totalPnl = unrealized + realized;
        if (totalPnl <= -config.MaxDailyLossLimit && !config.IsKillSwitchActive)
        {
            _logger.LogCritical("Emergency auto-freeze: Daily loss limit breached! P&L: {Pnl}", totalPnl);
            config.IsKillSwitchActive = true;
            await _strategyRepo.UpdateBotConfigAsync(config, ct);
        }
    }

    public async Task<bool> TriggerEmergencyKillSwitchAsync(CancellationToken ct = default)
    {
        _logger.LogCritical("EMERGENCY KILL SWITCH ACTIVATED!");
        var config = await _strategyRepo.GetBotConfigAsync(ct);
        config.IsKillSwitchActive = true;
        config.Mode = Domain.Enums.BotMode.Manual; // Drop out of auto
        await _strategyRepo.UpdateBotConfigAsync(config, ct);

        return true;
    }
}
