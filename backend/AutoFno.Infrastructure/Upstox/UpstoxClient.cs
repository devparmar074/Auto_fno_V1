using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AutoFno.Application.Exceptions;
using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;
using AutoFno.Infrastructure.Upstox.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoFno.Infrastructure.Upstox;

public class UpstoxClient : IUpstoxClient
{
    private readonly HttpClient _httpClient;
    private readonly UpstoxConfig _config;
    private readonly ILogger<UpstoxClient> _logger;

    public UpstoxClient(
        HttpClient httpClient,
        IOptions<UpstoxConfig> config,
        ILogger<UpstoxClient> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    public string GetAuthorizationUrl(string state)
    {
        var redirect = Uri.EscapeDataString(_config.RedirectUri);
        return $"{_config.AuthDialogUrl}?response_type=code&client_id={_config.ApiKey}&redirect_uri={redirect}&state={state}";
    }

    public async Task<UpstoxConnection> ExchangeCodeForTokenAsync(string code, CancellationToken ct = default)
    {
        var requestUrl = $"{_config.BaseUrl}/login/authorization/token";

        var body = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _config.ApiKey,
            ["client_secret"] = _config.ApiSecret,
            ["redirect_uri"] = _config.RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new FormUrlEncodedContent(body)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Upstox token exchange failed: {Status} {Response}", response.StatusCode, content);
            throw new TradingException($"Upstox token exchange failed: {response.StatusCode} - {content}");
        }

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new TradingException("Access token missing in Upstox response.");

        string? refreshToken = root.TryGetProperty("refresh_token", out var rProp) ? rProp.GetString() : null;
        string? userId = root.TryGetProperty("user_id", out var uProp) ? uProp.GetString() : null;
        string? userName = root.TryGetProperty("user_name", out var unProp) ? unProp.GetString() : null;
        string? email = root.TryGetProperty("email", out var eProp) ? eProp.GetString() : null;

        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
        var nextExpiryIst = nowIst.Date.AddDays(1).AddHours(3).AddMinutes(30);
        var expiresAtUtc = TimeZoneInfo.ConvertTimeToUtc(nextExpiryIst, istZone);

        return new UpstoxConnection
        {
            EncryptedAccessToken = TokenEncryptor.Encrypt(accessToken),
            EncryptedRefreshToken = refreshToken != null ? TokenEncryptor.Encrypt(refreshToken) : null,
            ExpiresAtUtc = expiresAtUtc,
            IsActive = true,
            UserId = userId,
            UserName = userName,
            Email = email,
            Broker = "UPSTOX",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<UpstoxAuthStatusDto> GetProfileAsync(string accessToken, CancellationToken ct = default)
    {
        var rawToken = TokenEncryptor.Decrypt(accessToken);
        var url = $"{_config.BaseUrl}/user/profile";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return new UpstoxAuthStatusDto { IsConnected = false };
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            return new UpstoxAuthStatusDto
            {
                IsConnected = true,
                UserId = data.TryGetProperty("user_id", out var u) ? u.GetString() : null,
                UserName = data.TryGetProperty("user_name", out var un) ? un.GetString() : null,
                Email = data.TryGetProperty("email", out var e) ? e.GetString() : null,
                Broker = "UPSTOX"
            };
        }

        return new UpstoxAuthStatusDto { IsConnected = true };
    }

    public async Task<List<string>> GetAvailableExpiriesAsync(string? accessToken, string instrumentKey, CancellationToken ct = default)
    {
        var encKey = Uri.EscapeDataString(instrumentKey);
        var url = $"{_config.BaseUrl}/option/contract?instrument_key={encKey}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return new List<string>();
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var expiries = new HashSet<string>();

        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                if (item.TryGetProperty("expiry", out var expProp))
                {
                    var expStr = expProp.GetString();
                    if (!string.IsNullOrEmpty(expStr))
                    {
                        expiries.Add(expStr);
                    }
                }
            }
        }

        return expiries.OrderBy(e => e).ToList();
    }

    public async Task<OptionChainDto?> GetOptionChainAsync(string? accessToken, string instrumentKey, string expiryDate, CancellationToken ct = default)
    {
        var encKey = Uri.EscapeDataString(instrumentKey);
        var encExpiry = Uri.EscapeDataString(expiryDate);
        var url = $"{_config.BaseUrl}/option/chain?instrument_key={encKey}&expiry_date={encExpiry}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Upstox option chain error: {Status}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        decimal spotPrice = 0;
        var strikes = new List<StrikeDataDto>();

        foreach (var item in data.EnumerateArray())
        {
            if (spotPrice == 0 && item.TryGetProperty("underlying_spot_price", out var spotProp))
            {
                spotPrice = spotProp.GetDecimal();
            }

            var strikePrice = item.TryGetProperty("strike_price", out var sp) ? sp.GetDecimal() : 0m;
            var strikePcr = item.TryGetProperty("pcr", out var pcrProp) ? pcrProp.GetDecimal() : 0m;

            OptionContractDetailDto? callDetail = null;
            if (item.TryGetProperty("call_options", out var callElem) && callElem.ValueKind == JsonValueKind.Object)
            {
                callDetail = ParseContractDetail(callElem, strikePrice, "CE");
            }

            OptionContractDetailDto? putDetail = null;
            if (item.TryGetProperty("put_options", out var putElem) && putElem.ValueKind == JsonValueKind.Object)
            {
                putDetail = ParseContractDetail(putElem, strikePrice, "PE");
            }

            strikes.Add(new StrikeDataDto
            {
                StrikePrice = strikePrice,
                StrikePcr = strikePcr,
                Call = callDetail,
                Put = putDetail
            });
        }

        return new OptionChainDto
        {
            UnderlyingKey = instrumentKey,
            UnderlyingSymbol = instrumentKey.Contains("SENSEX") ? "SENSEX" : "NIFTY50",
            SpotPrice = spotPrice,
            ExpiryDate = expiryDate,
            Strikes = strikes.OrderBy(s => s.StrikePrice).ToList(),
            TimestampUtc = DateTime.UtcNow
        };
    }

    private OptionContractDetailDto ParseContractDetail(JsonElement elem, decimal strikePrice, string optionType)
    {
        var key = elem.TryGetProperty("instrument_key", out var k) ? k.GetString() ?? "" : "";
        var detail = new OptionContractDetailDto
        {
            InstrumentKey = key,
            TradingSymbol = $"{strikePrice}_{optionType}"
        };

        if (elem.TryGetProperty("market_data", out var md))
        {
            detail.Ltp = md.TryGetProperty("ltp", out var ltp) ? ltp.GetDecimal() : 0m;
            detail.Volume = md.TryGetProperty("volume", out var v) ? v.GetInt64() : 0;
            detail.OpenInterest = md.TryGetProperty("oi", out var oi) ? (long)oi.GetDecimal() : 0;
            detail.PrevOpenInterest = md.TryGetProperty("prev_oi", out var poi) ? (long)poi.GetDecimal() : 0;
            detail.OiChange = detail.OpenInterest - detail.PrevOpenInterest;
            detail.OiChangePercent = detail.PrevOpenInterest > 0 
                ? Math.Round((decimal)detail.OiChange / detail.PrevOpenInterest * 100m, 2) 
                : 0m;
            detail.ClosePrice = md.TryGetProperty("close_price", out var cp) ? cp.GetDecimal() : 0m;
            detail.Change = detail.ClosePrice > 0 ? detail.Ltp - detail.ClosePrice : 0m;
            detail.ChangePercent = detail.ClosePrice > 0 ? Math.Round((detail.Change / detail.ClosePrice) * 100m, 2) : 0m;

            detail.BidPrice = md.TryGetProperty("bid_price", out var bp) ? bp.GetDecimal() : 0m;
            detail.BidQty = md.TryGetProperty("bid_qty", out var bq) ? bq.GetInt32() : 0;
            detail.AskPrice = md.TryGetProperty("ask_price", out var ap) ? ap.GetDecimal() : 0m;
            detail.AskQty = md.TryGetProperty("ask_qty", out var aq) ? aq.GetInt32() : 0;
        }

        if (elem.TryGetProperty("option_greeks", out var og))
        {
            detail.Greeks = new OptionGreeksDto
            {
                Delta = og.TryGetProperty("delta", out var d) ? Math.Round(d.GetDecimal(), 4) : 0m,
                Gamma = og.TryGetProperty("gamma", out var g) ? Math.Round(g.GetDecimal(), 6) : 0m,
                Theta = og.TryGetProperty("theta", out var th) ? Math.Round(th.GetDecimal(), 2) : 0m,
                Vega = og.TryGetProperty("vega", out var vg) ? Math.Round(vg.GetDecimal(), 2) : 0m,
                Rho = og.TryGetProperty("rho", out var rh) ? Math.Round(rh.GetDecimal(), 4) : 0m,
                Iv = og.TryGetProperty("iv", out var iv) ? Math.Round(iv.GetDecimal(), 2) : 0m,
                Pop = og.TryGetProperty("pop", out var pop) ? Math.Round(pop.GetDecimal(), 1) : 0m
            };
        }

        return detail;
    }

    private static decimal _cachedPrevCloseNifty = 22421.95m;
    private static decimal _cachedPrevCloseSensex = 71909.70m;

    public async Task<decimal> GetLtpAsync(string? accessToken, string instrumentKey, CancellationToken ct = default)
    {
        var quote = await GetSpotQuoteAsync(accessToken, instrumentKey, ct);
        return quote.ltp;
    }

    public async Task<(decimal ltp, decimal prevClose, decimal dayChange, decimal dayChangePercent)> GetSpotQuoteAsync(
        string? accessToken, 
        string instrumentKey, 
        CancellationToken ct = default)
    {
        var encKey = Uri.EscapeDataString(instrumentKey);
        bool isSensex = instrumentKey.Contains("SENSEX");

        // 1. Try Live Upstox Quote API if access token is available
        if (!string.IsNullOrEmpty(accessToken))
        {
            try
            {
                var url = $"{_config.BaseUrl}/market-quote/quotes?instrument_key={encKey}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        foreach (var prop in data.EnumerateObject())
                        {
                            var q = prop.Value;
                            decimal ltp = q.TryGetProperty("last_price", out var lp) ? lp.GetDecimal() : 0m;
                            decimal prevClose = 0m;
                            if (q.TryGetProperty("ohlc", out var ohlc) && ohlc.TryGetProperty("close", out var cp))
                            {
                                prevClose = cp.GetDecimal();
                            }
                            if (prevClose == 0)
                            {
                                prevClose = isSensex ? _cachedPrevCloseSensex : _cachedPrevCloseNifty;
                            }
                            decimal dayChange = q.TryGetProperty("net_change", out var nc) 
                                ? nc.GetDecimal() 
                                : (prevClose > 0 ? ltp - prevClose : 0m);
                            decimal dayChangePercent = prevClose > 0 ? Math.Round((dayChange / prevClose) * 100m, 2) : 0m;

                            if (ltp > 0)
                            {
                                return (ltp, prevClose, dayChange, dayChangePercent);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Upstox live market-quote failed: {Message}. Falling back to live intraday candle.", ex.Message);
            }
        }

        // 2. Real Live Intraday Candle fallback (Public, does not require token, returns actual live NSE/BSE ticks)
        try
        {
            var candles = await GetIntradayCandlesAsync(instrumentKey, "1minute", ct);
            if (candles != null && candles.Count > 0)
            {
                var latestCandle = candles[^1]; // Most recent 1-minute candle
                decimal ltp = latestCandle.Close;
                decimal prevClose = isSensex ? _cachedPrevCloseSensex : _cachedPrevCloseNifty;
                decimal dayChange = ltp - prevClose;
                decimal dayChangePercent = prevClose > 0 ? Math.Round((dayChange / prevClose) * 100m, 2) : 0m;

                return (ltp, prevClose, dayChange, dayChangePercent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed downloading live intraday candle for {Key}", instrumentKey);
        }

        decimal fallbackLtp = isSensex ? _cachedPrevCloseSensex : _cachedPrevCloseNifty;
        return (fallbackLtp, fallbackLtp, 0m, 0m);
    }

    public async Task<List<Candle>> GetIntradayCandlesAsync(string instrumentKey, string interval = "1minute", CancellationToken ct = default)
    {
        var encKey = Uri.EscapeDataString(instrumentKey);
        var url = $"{_config.BaseUrl}/historical-candle/intraday/{encKey}/{interval}";

        var result = new List<Candle>();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return result;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("candles", out var candles) &&
                candles.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in candles.EnumerateArray())
                {
                    if (item.GetArrayLength() >= 6)
                    {
                        var timeStr = item[0].GetString();
                        if (DateTime.TryParse(timeStr, out var parsed))
                        {
                            result.Add(new Candle
                            {
                                TimestampUtc = parsed.ToUniversalTime(),
                                Open = item[1].GetDecimal(),
                                High = item[2].GetDecimal(),
                                Low = item[3].GetDecimal(),
                                Close = item[4].GetDecimal(),
                                Volume = item[5].GetInt64(),
                                OpenInterest = item.GetArrayLength() > 6 ? item[6].GetInt64() : 0
                            });
                        }
                    }
                }
                result.Reverse(); // Ascending order
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed downloading candles from {Url}", url);
        }

        return result;
    }

    public async Task<OrderResultDto> PlaceOrderAsync(string accessToken, PlaceFnoOrderRequest request, CancellationToken ct = default)
    {
        var rawToken = TokenEncryptor.Decrypt(accessToken);
        var url = $"{_config.BaseUrl}/order/place";

        var payload = new Dictionary<string, object>
        {
            ["quantity"] = request.Quantity,
            ["product"] = request.ProductType == ProductType.I ? "I" : "D",
            ["validity"] = "DAY",
            ["price"] = request.OrderType == OrderType.MARKET ? 0 : request.Price,
            ["tag"] = request.CorrelationId ?? "AUTOFNO",
            ["instrument_token"] = request.InstrumentKey,
            ["order_type"] = request.OrderType.ToString(),
            ["transaction_type"] = request.TransactionType.ToString(),
            ["disclosed_quantity"] = 0,
            ["trigger_price"] = request.TriggerPrice,
            ["is_amo"] = false
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(req, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Upstox place order error: {Status} {Content}", response.StatusCode, content);
            return new OrderResultDto
            {
                Success = false,
                CorrelationId = request.CorrelationId,
                Status = OrderStatus.Rejected,
                Message = $"Upstox order rejected: {content}"
            };
        }

        using var doc = JsonDocument.Parse(content);
        string? orderId = null;
        if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("order_id", out var o))
        {
            orderId = o.GetString();
        }

        return new OrderResultDto
        {
            Success = true,
            OrderId = orderId,
            CorrelationId = request.CorrelationId,
            ExecutedPrice = request.Price,
            Status = OrderStatus.Submitted,
            Message = "Live order placed successfully."
        };
    }

    public async Task<bool> CancelOrderAsync(string accessToken, string orderId, CancellationToken ct = default)
    {
        var rawToken = TokenEncryptor.Decrypt(accessToken);
        var url = $"{_config.BaseUrl}/order/cancel?order_id={Uri.EscapeDataString(orderId)}";

        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(req, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<PositionDto>> GetPositionsAsync(string accessToken, CancellationToken ct = default)
    {
        var rawToken = TokenEncryptor.Decrypt(accessToken);
        var url = $"{_config.BaseUrl}/portfolio/short-term-positions";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(req, ct);
        var list = new List<PositionDto>();
        if (!response.IsSuccessStatusCode) return list;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                var key = item.TryGetProperty("instrument_token", out var k) ? k.GetString() ?? "" : "";
                var symbol = item.TryGetProperty("trading_symbol", out var s) ? s.GetString() ?? "" : "";
                var qty = item.TryGetProperty("quantity", out var q) ? q.GetInt32() : 0;
                var avg = item.TryGetProperty("average_price", out var a) ? a.GetDecimal() : 0m;
                var ltp = item.TryGetProperty("last_price", out var l) ? l.GetDecimal() : 0m;
                var pnl = item.TryGetProperty("pnl", out var p) ? p.GetDecimal() : 0m;
                var realized = item.TryGetProperty("realised", out var r) ? r.GetDecimal() : 0m;

                list.Add(new PositionDto
                {
                    InstrumentKey = key,
                    TradingSymbol = symbol,
                    IndexSymbol = symbol.Contains("SENSEX") ? "SENSEX" : "NIFTY50",
                    Quantity = qty,
                    AveragePrice = avg,
                    CurrentLtp = ltp,
                    UnrealizedPnl = pnl,
                    RealizedPnl = realized
                });
            }
        }

        return list;
    }

    public async Task<string?> GetRegisteredIpAsync(string accessToken, CancellationToken ct = default)
    {
        var rawToken = TokenEncryptor.Decrypt(accessToken);
        var url = $"{_config.BaseUrl}/user/ip";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(req, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> SetRegisteredIpAsync(string accessToken, string primaryIp, string? secondaryIp = null, CancellationToken ct = default)
    {
        var rawToken = TokenEncryptor.Decrypt(accessToken);
        var url = $"{_config.BaseUrl}/user/ip";

        var payload = new Dictionary<string, string?>
        {
            ["primary_ip"] = primaryIp.Trim()
        };
        if (!string.IsNullOrWhiteSpace(secondaryIp))
        {
            payload["secondary_ip"] = secondaryIp.Trim();
        }

        using var req = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(req, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new TradingException($"Failed updating static IP in Upstox: {content}");
        }

        return content;
    }
}
