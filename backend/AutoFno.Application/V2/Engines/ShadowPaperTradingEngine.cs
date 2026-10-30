using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.V2.Engines;

public class ShadowPaperTradingEngine : IShadowPaperTradingEngine
{
    private readonly ITradeManagementEngineV2 _tradeManagementEngine;
    private readonly IPerformanceAnalyticsEngine _analyticsEngine;
    private readonly ILogger<ShadowPaperTradingEngine> _logger;
    private readonly QuantitativeConvictionV2Config _config;

    private readonly List<V2HypotheticalPosition> _activePositions = new();
    private readonly List<V2HypotheticalTradeRecord> _closedTrades = new();
    private readonly HashSet<string> _processedEventKeys = new();
    private readonly object _syncLock = new();

    private readonly string _storageDir;
    private readonly string _positionsFile;
    private readonly string _tradesFile;
    private bool _isInitialized = false;

    public ShadowPaperTradingEngine(
        QuantitativeConvictionV2Config config,
        ITradeManagementEngineV2 tradeManagementEngine,
        IPerformanceAnalyticsEngine analyticsEngine,
        ILogger<ShadowPaperTradingEngine> logger)
    {
        _config = config;
        _tradeManagementEngine = tradeManagementEngine;
        _analyticsEngine = analyticsEngine;
        _logger = logger;

        _storageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "v2_shadow");
        _positionsFile = Path.Combine(_storageDir, "active_positions.json");
        _tradesFile = Path.Combine(_storageDir, "closed_trades.json");
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_isInitialized) return;

        lock (_syncLock)
        {
            try
            {
                if (!Directory.Exists(_storageDir))
                {
                    Directory.CreateDirectory(_storageDir);
                }

                // Restart Recovery of Persisted Hypothetical Positions
                if (File.Exists(_positionsFile))
                {
                    var json = File.ReadAllText(_positionsFile);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var restored = JsonSerializer.Deserialize<List<V2HypotheticalPosition>>(json);
                        if (restored != null)
                        {
                            _activePositions.Clear();
                            _activePositions.AddRange(restored.Where(p => p.IsOpen));
                            _logger.LogInformation("V2 Shadow Paper Trading restored {Count} active hypothetical positions from persistent storage.", _activePositions.Count);
                        }
                    }
                }

                // Restore historical closed trade records
                if (File.Exists(_tradesFile))
                {
                    var json = File.ReadAllText(_tradesFile);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var restoredTrades = JsonSerializer.Deserialize<List<V2HypotheticalTradeRecord>>(json);
                        if (restoredTrades != null)
                        {
                            _closedTrades.Clear();
                            _closedTrades.AddRange(restoredTrades);
                            foreach (var tr in restoredTrades)
                            {
                                _analyticsEngine.RecordTrade(tr);
                            }
                            _logger.LogInformation("V2 Shadow Paper Trading restored {Count} historical trade records.", _closedTrades.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restore persistent shadow trading state. Starting with clean memory.");
            }
            finally
            {
                _isInitialized = true;
            }
        }
        await Task.CompletedTask;
    }

    public Task ProcessLiveSnapshotAsync(
        OptionChainDto optionChain,
        V2ConvictionScoreDto score,
        V2TradeCandidate candidate,
        CancellationToken ct = default)
    {
        if (optionChain == null) return Task.CompletedTask;

        lock (_syncLock)
        {
            var now = DateTime.UtcNow;

            // 1. Event Deduplication Check
            string eventKey = $"{optionChain.UnderlyingSymbol}_{optionChain.SpotPrice}_{optionChain.TimestampUtc:yyyyMMddHHmmss}";
            if (_processedEventKeys.Contains(eventKey))
            {
                return Task.CompletedTask; // Duplicate quote snapshot ignored
            }
            _processedEventKeys.Add(eventKey);
            if (_processedEventKeys.Count > 5000) _processedEventKeys.Clear();

            // 2. Manage existing open positions
            for (int i = _activePositions.Count - 1; i >= 0; i--)
            {
                var pos = _activePositions[i];
                if (!pos.IsOpen) continue;

                decimal currentOptionPrice = pos.CurrentPrice;
                var strikeObj = optionChain.Strikes?.FirstOrDefault(s => s.StrikePrice == pos.TargetStrike);
                if (strikeObj != null)
                {
                    var contract = pos.OptionType == OptionType.CE ? strikeObj.Call : strikeObj.Put;
                    if (contract != null && contract.Ltp > 0)
                    {
                        currentOptionPrice = contract.Ltp;
                    }
                }

                _tradeManagementEngine.UpdatePosition(pos, optionChain.SpotPrice, currentOptionPrice, now, _config, out var exitReason);

                // Cutoff time (15:15 IST)
                var istTime = now.AddHours(5).AddMinutes(30);
                if (exitReason == V2ExitReason.None && istTime.TimeOfDay >= new TimeSpan(15, 15, 0))
                {
                    exitReason = V2ExitReason.TimeCutoffExit;
                }

                if (exitReason != V2ExitReason.None)
                {
                    var closedRecord = _tradeManagementEngine.ClosePosition(pos, currentOptionPrice, exitReason, now, _config);
                    _closedTrades.Add(closedRecord);
                    _analyticsEngine.RecordTrade(closedRecord);
                    _activePositions.RemoveAt(i);

                    _logger.LogInformation("V2 Shadow Trade Closed: {Symbol} Exit={Price} PnL={Pnl} Reason={Reason}",
                        closedRecord.TradingSymbol, closedRecord.ExitPrice, closedRecord.NetPnl, closedRecord.ExitReason);
                }
            }

            // 3. Open new shadow position if qualified
            if (candidate != null && candidate.IsQualified && candidate.AllocatedLots > 0 &&
                _activePositions.Count(p => p.IsOpen) < _config.MaxSimultaneousPositions)
            {
                var newPos = new V2HypotheticalPosition
                {
                    IndexSymbol = candidate.IndexSymbol,
                    InstrumentKey = candidate.InstrumentKey,
                    TradingSymbol = candidate.TradingSymbol,
                    TargetStrike = candidate.TargetStrike,
                    OptionType = candidate.OptionType,
                    Lots = candidate.AllocatedLots,
                    Quantity = candidate.AllocatedQuantity,
                    RemainingQuantity = candidate.AllocatedQuantity,
                    EntryPrice = candidate.ExpectedEntryPrice,
                    CurrentPrice = candidate.ExpectedEntryPrice,
                    PeakPrice = candidate.ExpectedEntryPrice,
                    TroughPrice = candidate.ExpectedEntryPrice,
                    StopLossPrice = candidate.StopLossPrice,
                    Target1Price = candidate.Target1Price,
                    Target2Price = candidate.Target2Price,
                    InitialRiskPerLot = (candidate.ExpectedEntryPrice - candidate.StopLossPrice) + candidate.EstimatedCostPerLot,
                    EntryTimeUtc = now,
                    LastUpdatedUtc = now,
                    IsOpen = true
                };

                _activePositions.Add(newPos);
                _logger.LogInformation("V2 Shadow Trade Opened: {Symbol} {Lots} Lots @ Rs {Price}",
                    newPos.TradingSymbol, newPos.Lots, newPos.EntryPrice);
            }

            // 4. Save state asynchronously to disk
            PersistStateUnsafe();
        }

        return Task.CompletedTask;
    }

    private void PersistStateUnsafe()
    {
        try
        {
            if (!Directory.Exists(_storageDir)) Directory.CreateDirectory(_storageDir);
            File.WriteAllText(_positionsFile, JsonSerializer.Serialize(_activePositions));
            File.WriteAllText(_tradesFile, JsonSerializer.Serialize(_closedTrades.TakeLast(500)));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to persist shadow positions file.");
        }
    }

    public Task<List<V2HypotheticalPosition>> GetActivePositionsAsync()
    {
        lock (_syncLock)
        {
            return Task.FromResult(_activePositions.Where(p => p.IsOpen).ToList());
        }
    }

    public Task<List<V2HypotheticalTradeRecord>> GetHistoricalTradesAsync(int limit = 100)
    {
        lock (_syncLock)
        {
            return Task.FromResult(_closedTrades.TakeLast(limit).ToList());
        }
    }

    public V2PerformanceMetricsDto GetMetrics() => _analyticsEngine.GetPerformanceMetrics();
}
