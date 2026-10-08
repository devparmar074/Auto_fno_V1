using AutoFno.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoFno.Infrastructure.BackgroundServices;

public class FnOMarketFeedBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FnOMarketFeedBackgroundService> _logger;
    private int _tickCount = 0;

    public FnOMarketFeedBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FnOMarketFeedBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FnOMarketFeedBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var marketDataService = scope.ServiceProvider.GetRequiredService<IFnOMarketDataService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<ITradingNotificationService>();

                // Check if market is currently open
                if (!marketDataService.IsMarketOpen())
                {
                    // When market is closed (weekends, holidays, or outside 09:15-15:30 IST),
                    // exchange does not generate live ticks. Idle peacefully.
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                var activeIndex = marketDataService.ActiveIndex;
                var ltp = await marketDataService.GetSpotLtpAsync(activeIndex, stoppingToken);

                bool isSensex = activeIndex.Contains("SENSEX");
                decimal prevClose = isSensex ? 71909.70m : 22421.95m;
                decimal dayChange = ltp - prevClose;
                decimal dayChangePercent = prevClose > 0 ? Math.Round((dayChange / prevClose) * 100m, 2) : 0m;

                // Broadcast real exchange spot quote tick
                await notificationService.BroadcastSpotQuoteAsync(activeIndex, ltp, dayChange, dayChangePercent);

                _tickCount++;

                // Broadcast full option chain and PCR every 3 seconds to avoid network saturation
                if (_tickCount % 3 == 0)
                {
                    var chain = await marketDataService.GetOptionChainAsync(activeIndex, null, stoppingToken);
                    await notificationService.BroadcastOptionChainAsync(chain);

                    var pcr = await marketDataService.GetPcrSummaryAsync(activeIndex, stoppingToken);
                    var maxPain = await marketDataService.GetMaxPainAsync(activeIndex, stoppingToken);
                    await notificationService.BroadcastPcrMetricsAsync(pcr, maxPain);
                }

                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FnOMarketFeedBackgroundService loop.");
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("FnOMarketFeedBackgroundService stopping.");
    }
}
