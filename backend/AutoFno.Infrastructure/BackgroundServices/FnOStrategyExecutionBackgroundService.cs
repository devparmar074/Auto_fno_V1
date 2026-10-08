using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoFno.Infrastructure.BackgroundServices;

public class FnOStrategyExecutionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FnOStrategyExecutionBackgroundService> _logger;
    private DateTime _lastExecutionTime = DateTime.MinValue;

    public FnOStrategyExecutionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FnOStrategyExecutionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FnOStrategyExecutionBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var marketDataService = scope.ServiceProvider.GetRequiredService<IFnOMarketDataService>();
                var strategyEngine = scope.ServiceProvider.GetRequiredService<IFnOStrategyEngine>();
                var strategyRepo = scope.ServiceProvider.GetRequiredService<IFnOStrategyRepository>();
                var riskManager = scope.ServiceProvider.GetRequiredService<IFnORiskManager>();
                var orderService = scope.ServiceProvider.GetRequiredService<IFnOOrderService>();
                var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<ITradingNotificationService>();

                var config = await strategyRepo.GetBotConfigAsync(stoppingToken);

                // Run risk manager background checks (daily loss limit, auto square-off cutoff)
                await riskManager.CheckRiskLimitsAndCutoffAsync(config, stoppingToken);

                var activeIndex = marketDataService.ActiveIndex;
                var chain = await marketDataService.GetOptionChainAsync(activeIndex, null, stoppingToken);
                var pcr = await marketDataService.GetPcrSummaryAsync(activeIndex, stoppingToken);
                var maxPain = await marketDataService.GetMaxPainAsync(activeIndex, stoppingToken);
                var candles = await marketDataService.GetSpotCandlesAsync(activeIndex, "1minute", stoppingToken);

                // Evaluate Quantitative Conviction Score (-100 to +100)
                var score = await strategyEngine.EvaluateSetupAsync(chain, pcr, maxPain, candles, stoppingToken);

                // Broadcast live score to clients
                await notificationService.BroadcastStrategyScoreAsync(score);

                // Persist signal history
                await strategyRepo.SaveSignalAsync(new StrategySignalHistory
                {
                    IndexSymbol = activeIndex,
                    SpotPrice = score.SpotPrice,
                    TotalScore = score.TotalScore,
                    PcrScore = score.PcrScore,
                    OiMaxPainScore = score.OiMaxPainScore,
                    GreeksIvScore = score.GreeksIvScore,
                    SpotTrendScore = score.SpotTrendScore,
                    Recommendation = score.Recommendation,
                    RecommendedStrike = score.RecommendedStrike,
                    RecommendedOptionType = score.RecommendedOptionType,
                    SetupRationale = score.SetupRationale,
                    CreatedAtUtc = DateTime.UtcNow
                }, stoppingToken);

                // Check Bot Execution Modes
                if (!config.IsKillSwitchActive && (DateTime.UtcNow - _lastExecutionTime).TotalMinutes >= 2)
                {
                    if (config.Mode == BotMode.FullAuto)
                    {
                        if (score.Recommendation == StrategyRecommendation.StrongBuyCall ||
                            score.Recommendation == StrategyRecommendation.StrongBuyPut)
                        {
                            await TryAutoExecuteAsync(score, config, orderService, stoppingToken);
                        }
                    }
                    else if (config.Mode == BotMode.SemiAuto)
                    {
                        if (score.Recommendation == StrategyRecommendation.StrongBuyCall ||
                            score.Recommendation == StrategyRecommendation.StrongBuyPut)
                        {
                            // Push 1-click execution alert to UI
                            await notificationService.BroadcastSemiAutoAlertAsync(score);
                        }
                    }
                }

                // Push position and P&L updates
                var positions = await portfolioService.GetPositionsAsync(stoppingToken);
                var (unrealized, realized) = await portfolioService.GetPnlSummaryAsync(stoppingToken);
                await notificationService.BroadcastPositionsAsync(positions, unrealized, realized);

                await Task.Delay(4000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FnOStrategyExecutionBackgroundService loop.");
                await Task.Delay(4000, stoppingToken);
            }
        }

        _logger.LogInformation("FnOStrategyExecutionBackgroundService stopping.");
    }

    private async Task TryAutoExecuteAsync(FnOStrategyScoreDto score, BotConfig config, IFnOOrderService orderService, CancellationToken ct)
    {
        _logger.LogInformation("Full-Auto triggering order execution for {Symbol}: {Rec} at {Strike}",
            score.IndexSymbol, score.Recommendation, score.RecommendedStrike);

        try
        {
            var req = new PlaceFnoOrderRequest
            {
                IndexSymbol = score.IndexSymbol,
                TradingSymbol = $"{score.IndexSymbol}_{score.RecommendedStrike}",
                InstrumentKey = $"{score.IndexSymbol}_{score.RecommendedStrike}",
                OptionType = score.RecommendedOptionType,
                TransactionType = score.RecommendedAction,
                OrderType = OrderType.MARKET,
                ProductType = ProductType.I,
                Lots = config.DefaultLots,
                Price = score.RecommendedEntryPrice,
                StopLossPrice = score.RecommendedStopLoss,
                TakeProfitPrice = score.RecommendedTarget,
                PlacedByMode = BotMode.FullAuto,
                CorrelationId = $"AUTO_{Guid.NewGuid():N}"
            };

            var result = await orderService.PlaceOrderAsync(req, ct);
            if (result.Success)
            {
                _lastExecutionTime = DateTime.UtcNow;
                _logger.LogInformation("Full-Auto Order executed successfully: OrderId {OrderId}", result.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Full-Auto Order placement failed: {Message}", ex.Message);
        }
    }
}
