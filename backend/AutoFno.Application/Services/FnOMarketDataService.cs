using AutoFno.Application.Interfaces;
using AutoFno.Application.Services.Mathematics;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.Services;

public class FnOMarketDataService : IFnOMarketDataService
{
    private readonly IUpstoxClient _upstoxClient;
    private readonly IUpstoxAuthService _authService;
    private readonly IFnOStrategyEngine _strategyEngine;
    private readonly IPortfolioService _portfolioService;
    private readonly IFnOStrategyRepository _strategyRepo;
    private readonly ILogger<FnOMarketDataService> _logger;

    private static string _activeIndex = "NIFTY50";
    private static string? _selectedExpiry = null;

    public string ActiveIndex 
    { 
        get => _activeIndex; 
        set => _activeIndex = value; 
    }

    public string? SelectedExpiry 
    { 
        get => _selectedExpiry; 
        set => _selectedExpiry = value; 
    }

    private static decimal _cachedNiftySpot = 22497.10m;
    private static decimal _cachedSensexSpot = 72150.00m;
    private static readonly Random _rnd = new();

    public FnOMarketDataService(
        IUpstoxClient upstoxClient,
        IUpstoxAuthService authService,
        IFnOStrategyEngine strategyEngine,
        IPortfolioService portfolioService,
        IFnOStrategyRepository strategyRepo,
        ILogger<FnOMarketDataService> logger)
    {
        _upstoxClient = upstoxClient;
        _authService = authService;
        _strategyEngine = strategyEngine;
        _portfolioService = portfolioService;
        _strategyRepo = strategyRepo;
        _logger = logger;
    }

    public string ResolveUnderlyingKey(string? indexSymbol)
    {
        var sym = (indexSymbol ?? ActiveIndex).ToUpperInvariant();
        if (sym.Contains("SENSEX") || sym.Contains("BSE"))
        {
            return "BSE_INDEX|SENSEX";
        }
        return "NSE_INDEX|Nifty 50";
    }

    public async Task<List<string>> GetAvailableExpiriesAsync(string? indexSymbol = null, CancellationToken ct = default)
    {
        var key = ResolveUnderlyingKey(indexSymbol);
        try
        {
            string? token = null;
            var auth = await _authService.GetStatusAsync(ct);
            if (auth.IsConnected)
            {
                token = await _authService.GetActiveAccessTokenAsync(ct);
            }

            var expiries = await _upstoxClient.GetAvailableExpiriesAsync(token, key, ct);
            if (expiries.Count > 0)
            {
                return expiries;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed fetching live expiries: {Message}. Using dynamic expiries.", ex.Message);
        }

        // Fallback: Generate upcoming Thursdays/Fridays
        return GenerateUpcomingExpiries(indexSymbol);
    }

    public async Task<OptionChainDto> GetOptionChainAsync(string? indexSymbol = null, string? expiryDate = null, CancellationToken ct = default)
    {
        var sym = (indexSymbol ?? ActiveIndex).ToUpperInvariant();
        var key = ResolveUnderlyingKey(sym);
        var expiries = await GetAvailableExpiriesAsync(sym, ct);
        var expiry = expiryDate ?? SelectedExpiry ?? (expiries.Count > 0 ? expiries[0] : DateTime.UtcNow.ToString("yyyy-MM-dd"));

        string? token = null;
        var auth = await _authService.GetStatusAsync(ct);
        if (auth.IsConnected)
        {
            token = await _authService.GetActiveAccessTokenAsync(ct);
        }

        OptionChainDto? chain = null;

        try
        {
            chain = await _upstoxClient.GetOptionChainAsync(token, key, expiry, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Live option chain fetch failed: {Message}. Generating simulated chain.", ex.Message);
        }

        if (chain == null || chain.Strikes.Count == 0)
        {
            chain = GenerateSimulatedOptionChain(sym, key, expiry, expiries);
        }
        else
        {
            chain.AvailableExpiries = expiries;
        }

        // Ensure Greeks & PCR & MaxPain are calculated on the chain
        EnrichOptionChainAnalytics(chain, expiry);

        return chain;
    }

    public async Task<PcrSummaryDto> GetPcrSummaryAsync(string? indexSymbol = null, CancellationToken ct = default)
    {
        var chain = await GetOptionChainAsync(indexSymbol, null, ct);
        return PcrCalculator.CalculatePcrSummary(chain.Strikes, chain.SpotPrice);
    }

    public async Task<MaxPainDto> GetMaxPainAsync(string? indexSymbol = null, CancellationToken ct = default)
    {
        var chain = await GetOptionChainAsync(indexSymbol, null, ct);
        return MaxPainCalculator.CalculateMaxPain(chain.Strikes, chain.SpotPrice, chain.ExpiryDate);
    }

    public async Task<decimal> GetSpotLtpAsync(string? indexSymbol = null, CancellationToken ct = default)
    {
        var sym = (indexSymbol ?? ActiveIndex).ToUpperInvariant();
        var key = ResolveUnderlyingKey(sym);

        // When market is closed (Saturdays, Sundays, or outside 09:15-15:30 IST),
        // spot prices remain static and frozen at previous market close.
        if (!IsMarketOpen())
        {
            return sym.Contains("SENSEX") ? _cachedSensexSpot : _cachedNiftySpot;
        }

        try
        {
            string? token = null;
            var auth = await _authService.GetStatusAsync(ct);
            if (auth.IsConnected)
            {
                token = await _authService.GetActiveAccessTokenAsync(ct);
            }

            var (ltp, _, _, _) = await _upstoxClient.GetSpotQuoteAsync(token, key, ct);
            if (ltp > 0)
            {
                if (sym.Contains("SENSEX"))
                {
                    _cachedSensexSpot = ltp;
                }
                else
                {
                    _cachedNiftySpot = ltp;
                }
                return ltp;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed fetching live spot quote for {Symbol}: {Message}", sym, ex.Message);
        }

        return sym.Contains("SENSEX") ? _cachedSensexSpot : _cachedNiftySpot;
    }

    public async Task<List<Candle>> GetSpotCandlesAsync(string? indexSymbol = null, string interval = "1minute", CancellationToken ct = default)
    {
        var sym = (indexSymbol ?? ActiveIndex).ToUpperInvariant();
        var key = ResolveUnderlyingKey(sym);

        try
        {
            var candles = await _upstoxClient.GetIntradayCandlesAsync(key, interval, ct);
            if (candles != null && candles.Count >= 20)
            {
                return candles;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Live candles fetch failed: {Message}. Generating simulated intraday candles.", ex.Message);
        }

        return GenerateSimulatedCandles(sym);
    }

    public async Task<DashboardFnoSummaryDto> GetDashboardSummaryAsync(string? indexSymbol = null, CancellationToken ct = default)
    {
        var sym = indexSymbol ?? ActiveIndex;
        var key = ResolveUnderlyingKey(sym);

        string? token = null;
        var auth = await _authService.GetStatusAsync(ct);
        if (auth.IsConnected)
        {
            token = await _authService.GetActiveAccessTokenAsync(ct);
        }

        var (realLtp, realPrevClose, realChange, realChangePct) = await _upstoxClient.GetSpotQuoteAsync(token, key, ct);
        if (realLtp > 0)
        {
            if (sym.Contains("SENSEX")) _cachedSensexSpot = realLtp;
            else _cachedNiftySpot = realLtp;
        }

        var chain = await GetOptionChainAsync(sym, null, ct);
        if (realLtp > 0)
        {
            chain.SpotPrice = realLtp;
            chain.PrevSpotPrice = realPrevClose;
            chain.SpotDayChange = realChange;
            chain.SpotDayChangePercent = realChangePct;
        }

        var pcr = PcrCalculator.CalculatePcrSummary(chain.Strikes, chain.SpotPrice);
        var maxPain = MaxPainCalculator.CalculateMaxPain(chain.Strikes, chain.SpotPrice, chain.ExpiryDate);
        var candles = await GetSpotCandlesAsync(sym, "1minute", ct);
        var score = await _strategyEngine.EvaluateSetupAsync(chain, pcr, maxPain, candles, ct);
        var positions = await _portfolioService.GetPositionsAsync(ct);
        var (unrealized, realized) = await _portfolioService.GetPnlSummaryAsync(ct);
        var config = await _strategyRepo.GetBotConfigAsync(ct);

        return new DashboardFnoSummaryDto
        {
            IndexSymbol = sym,
            SpotPrice = chain.SpotPrice,
            PrevSpotPrice = chain.PrevSpotPrice,
            SpotDayChange = chain.SpotDayChange,
            SpotDayChangePercent = chain.SpotDayChangePercent,
            OptionChain = chain,
            PcrSummary = pcr,
            MaxPain = maxPain,
            StrategyScore = score,
            OpenPositions = positions,
            TotalUnrealizedPnl = unrealized,
            TodayRealizedPnl = realized,
            BotConfig = config,
            MarketStatus = IsMarketOpen() ? "OPEN" : "CLOSED",
            IsTokenValid = auth.IsConnected,
            ServerTimeUtc = DateTime.UtcNow
        };
    }

    public bool IsMarketOpen()
    {
        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

        if (nowIst.DayOfWeek == DayOfWeek.Saturday || nowIst.DayOfWeek == DayOfWeek.Sunday)
        {
            return false;
        }

        var openTime = new TimeSpan(9, 15, 0);
        var closeTime = new TimeSpan(15, 30, 0);
        return nowIst.TimeOfDay >= openTime && nowIst.TimeOfDay <= closeTime;
    }

    private void EnrichOptionChainAnalytics(OptionChainDto chain, string expiry)
    {
        var spot = chain.SpotPrice;
        if (spot <= 0) return;

        // Calculate days to expiry
        double daysToExpiry = 4.0;
        if (DateTime.TryParse(expiry, out var expDate))
        {
            daysToExpiry = Math.Max(0.1, (expDate.Date - DateTime.UtcNow.Date).TotalDays);
        }

        // Find ATM Strike
        var closest = chain.Strikes.OrderBy(s => Math.Abs(s.StrikePrice - spot)).FirstOrDefault();
        if (closest != null)
        {
            closest.IsAtm = true;
            chain.AtmStrike = closest.StrikePrice;
        }

        // Fill in Greeks via Black-Scholes if missing
        foreach (var s in chain.Strikes)
        {
            if (s.Call != null)
            {
                if (s.Call.Greeks == null || s.Call.Greeks.Delta == 0)
                {
                    s.Call.Greeks = BlackScholesGreeksCalculator.CalculateGreeks(
                        spot, s.StrikePrice, daysToExpiry, OptionType.CE, s.Call.Ltp, s.Call.Greeks?.Iv ?? 0);
                }
            }

            if (s.Put != null)
            {
                if (s.Put.Greeks == null || s.Put.Greeks.Delta == 0)
                {
                    s.Put.Greeks = BlackScholesGreeksCalculator.CalculateGreeks(
                        spot, s.StrikePrice, daysToExpiry, OptionType.PE, s.Put.Ltp, s.Put.Greeks?.Iv ?? 0);
                }
            }
        }

        // Calculate Max Pain & Walls
        var maxPain = MaxPainCalculator.CalculateMaxPain(chain.Strikes, spot, expiry);
        chain.MaxPainStrike = maxPain.MaxPainStrike;

        var callWall = chain.Strikes.Where(s => s.Call != null).OrderByDescending(s => s.Call!.OpenInterest).FirstOrDefault();
        if (callWall != null)
        {
            callWall.IsCallWall = true;
            chain.CallWallStrike = callWall.StrikePrice;
        }

        var putWall = chain.Strikes.Where(s => s.Put != null).OrderByDescending(s => s.Put!.OpenInterest).FirstOrDefault();
        if (putWall != null)
        {
            putWall.IsPutWall = true;
            chain.PutWallStrike = putWall.StrikePrice;
        }

        // PCR
        var pcr = PcrCalculator.CalculatePcrSummary(chain.Strikes, spot);
        chain.OiPcr = pcr.OiPcr;
        chain.VolumePcr = pcr.VolumePcr;
        chain.TotalCallOi = pcr.TotalCallOi;
        chain.TotalPutOi = pcr.TotalPutOi;
    }

    private OptionChainDto GenerateSimulatedOptionChain(string sym, string underlyingKey, string expiry, List<string> availableExpiries)
    {
        bool isSensex = sym.Contains("SENSEX");
        decimal spot = isSensex ? _cachedSensexSpot : _cachedNiftySpot;
        decimal prevSpot = isSensex ? 71909.70m : 22421.95m;
        decimal dayChange = spot - prevSpot;
        decimal dayChangePct = prevSpot > 0 ? Math.Round((dayChange / prevSpot) * 100m, 2) : 0m;

        decimal strikeStep = isSensex ? 100m : 50m;
        decimal baseAtm = Math.Round(spot / strikeStep) * strikeStep;

        var strikes = new List<StrikeDataDto>();
        double daysToExpiry = 3.5;

        // Generate +/- 15 strikes around ATM
        for (int i = -15; i <= 15; i++)
        {
            decimal strike = baseAtm + (i * strikeStep);
            bool isAtm = (i == 0);

            // Synthetic realistic options prices
            double sD = (double)spot;
            double kD = (double)strike;
            double sigma = 0.13 + (Math.Abs(i) * 0.002); // IV smile

            decimal callPrice = Math.Max(0.50m, Math.Round((decimal)BlackScholesGreeksCalculator.BlackScholesPrice(sD, kD, daysToExpiry / 365.0, sigma, OptionType.CE), 2));
            decimal putPrice = Math.Max(0.50m, Math.Round((decimal)BlackScholesGreeksCalculator.BlackScholesPrice(sD, kD, daysToExpiry / 365.0, sigma, OptionType.PE), 2));

            long callOi = (long)(Math.Max(1000, 150000 - Math.Abs(i) * 8000 + _rnd.Next(-5000, 15000)));
            long putOi = (long)(Math.Max(1000, 140000 - Math.Abs(i) * 7500 + _rnd.Next(-5000, 15000)));

            long callVol = (long)(callOi * (0.3 + _rnd.NextDouble() * 0.5));
            long putVol = (long)(putOi * (0.3 + _rnd.NextDouble() * 0.5));

            long callOiChg = (long)(_rnd.Next(-15000, 25000));
            long putOiChg = (long)(_rnd.Next(-10000, 30000));

            var callGreeks = BlackScholesGreeksCalculator.CalculateGreeks(spot, strike, daysToExpiry, OptionType.CE, callPrice, (decimal)(sigma * 100));
            var putGreeks = BlackScholesGreeksCalculator.CalculateGreeks(spot, strike, daysToExpiry, OptionType.PE, putPrice, (decimal)(sigma * 100));

            strikes.Add(new StrikeDataDto
            {
                StrikePrice = strike,
                IsAtm = isAtm,
                Call = new OptionContractDetailDto
                {
                    InstrumentKey = $"{underlyingKey}_{strike}_CE",
                    TradingSymbol = $"{sym}_{strike}_CE",
                    Ltp = callPrice,
                    PrevLtp = callPrice - 2.5m,
                    Change = 2.5m,
                    ChangePercent = 3.2m,
                    Volume = callVol,
                    OpenInterest = callOi,
                    PrevOpenInterest = callOi - callOiChg,
                    OiChange = callOiChg,
                    OiChangePercent = callOi > 0 ? Math.Round((decimal)callOiChg / (decimal)callOi * 100m, 2) : 0,
                    BidPrice = Math.Max(0.5m, callPrice - 0.25m),
                    BidQty = 450,
                    AskPrice = callPrice + 0.25m,
                    AskQty = 450,
                    Greeks = callGreeks,
                    Buildup = PcrCalculator.ClassifyBuildup(2.5m, callOiChg)
                },
                Put = new OptionContractDetailDto
                {
                    InstrumentKey = $"{underlyingKey}_{strike}_PE",
                    TradingSymbol = $"{sym}_{strike}_PE",
                    Ltp = putPrice,
                    PrevLtp = putPrice + 1.8m,
                    Change = -1.8m,
                    ChangePercent = -2.7m,
                    Volume = putVol,
                    OpenInterest = putOi,
                    PrevOpenInterest = putOi - putOiChg,
                    OiChange = putOiChg,
                    OiChangePercent = putOi > 0 ? Math.Round((decimal)putOiChg / (decimal)putOi * 100m, 2) : 0,
                    BidPrice = Math.Max(0.5m, putPrice - 0.25m),
                    BidQty = 450,
                    AskPrice = putPrice + 0.25m,
                    AskQty = 450,
                    Greeks = putGreeks,
                    Buildup = PcrCalculator.ClassifyBuildup(-1.8m, putOiChg)
                }
            });
        }

        return new OptionChainDto
        {
            UnderlyingKey = underlyingKey,
            UnderlyingSymbol = sym,
            SpotPrice = spot,
            PrevSpotPrice = prevSpot,
            SpotDayChange = dayChange,
            SpotDayChangePercent = dayChangePct,
            ExpiryDate = expiry,
            AvailableExpiries = availableExpiries,
            Strikes = strikes,
            TimestampUtc = DateTime.UtcNow
        };
    }

    private List<Candle> GenerateSimulatedCandles(string sym)
    {
        var result = new List<Candle>();
        decimal current = sym.Contains("SENSEX") ? _cachedSensexSpot : _cachedNiftySpot;
        decimal price = current - 50m;
        var now = DateTime.UtcNow;

        for (int i = 60; i >= 0; i--)
        {
            var time = now.AddMinutes(-i);
            var delta = (decimal)(_rnd.NextDouble() * 6.0 - 2.8);
            price += delta;
            var open = price;
            var close = price + (decimal)(_rnd.NextDouble() * 3.0 - 1.2);
            var high = Math.Max(open, close) + (decimal)(_rnd.NextDouble() * 2.0);
            var low = Math.Min(open, close) - (decimal)(_rnd.NextDouble() * 2.0);
            var vol = _rnd.Next(25000, 120000);

            result.Add(new Candle
            {
                TimestampUtc = time,
                Open = Math.Round(open, 2),
                High = Math.Round(high, 2),
                Low = Math.Round(low, 2),
                Close = Math.Round(close, 2),
                Volume = vol,
                OpenInterest = 0
            });
        }

        return result;
    }

    private List<string> GenerateUpcomingExpiries(string? indexSymbol)
    {
        var list = new List<string>();
        var now = DateTime.UtcNow;
        var sym = (indexSymbol ?? ActiveIndex).ToUpperInvariant();

        // Nifty weekly expiry: Thursdays. Sensex weekly expiry: Fridays.
        var targetDay = sym.Contains("SENSEX") ? DayOfWeek.Friday : DayOfWeek.Thursday;

        for (int i = 0; i < 30; i++)
        {
            var date = now.AddDays(i);
            if (date.DayOfWeek == targetDay)
            {
                list.Add(date.ToString("yyyy-MM-dd"));
                if (list.Count >= 4) break;
            }
        }

        return list;
    }
}
