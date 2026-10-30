using System;
using System.Collections.Generic;
using System.Linq;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.Models;

namespace AutoFno.Application.V2.Engines;

public class MarketDataValidationEngine : IMarketDataValidationEngine
{
    public bool ValidateMarketData(OptionChainDto? optionChain, List<Candle>? spotCandles, out List<string> validationErrors)
    {
        validationErrors = new List<string>();

        if (optionChain == null)
        {
            validationErrors.Add("Option chain data is null.");
            return false;
        }

        if (optionChain.SpotPrice <= 0)
        {
            validationErrors.Add($"Invalid underlying spot price: {optionChain.SpotPrice}. Must be positive.");
        }

        if (optionChain.Strikes == null || optionChain.Strikes.Count < 6)
        {
            validationErrors.Add($"Insufficient strike count ({optionChain.Strikes?.Count ?? 0}). Minimum 6 strikes required.");
        }
        else
        {
            // Verify ATM strike exists or can be determined
            var hasValidStrikes = optionChain.Strikes.Any(s => s.StrikePrice > 0 && (s.Call != null || s.Put != null));
            if (!hasValidStrikes)
            {
                validationErrors.Add("No valid Call/Put contracts found across strikes.");
            }
        }

        if (spotCandles == null || spotCandles.Count < 21)
        {
            validationErrors.Add($"Insufficient candlestick history ({spotCandles?.Count ?? 0} candles). Minimum 21 candles required for EMA/RSI indicators.");
        }
        else
        {
            // Verify OHLC validity on recent candles
            for (int i = Math.Max(0, spotCandles.Count - 5); i < spotCandles.Count; i++)
            {
                var c = spotCandles[i];
                if (c.High < c.Low || c.High < c.Open || c.High < c.Close || c.Low > c.Open || c.Low > c.Close)
                {
                    validationErrors.Add($"Invalid OHLC structure on candle at {c.TimestampUtc}: High={c.High}, Low={c.Low}, Open={c.Open}, Close={c.Close}.");
                    break;
                }
            }
        }

        return validationErrors.Count == 0;
    }
}
