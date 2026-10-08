using AutoFno.Domain.Entities;
using AutoFno.Domain.Models;

namespace AutoFno.Application.Interfaces;

public interface IFnORiskManager
{
    Task<bool> CanExecuteNewTradeAsync(PlaceFnoOrderRequest request, BotConfig config, CancellationToken ct = default);
    Task<int> ResolveLotSizeAsync(string indexSymbol, CancellationToken ct = default);
    Task CheckRiskLimitsAndCutoffAsync(BotConfig config, CancellationToken ct = default);
    Task<bool> TriggerEmergencyKillSwitchAsync(CancellationToken ct = default);
}
