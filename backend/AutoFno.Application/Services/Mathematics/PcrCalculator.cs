using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;

namespace AutoFno.Application.Services.Mathematics;

public static class PcrCalculator
{
    /// <summary>
    /// Computes Put-Call Ratio and sentiment summary across strikes.
    /// </summary>
    public static PcrSummaryDto CalculatePcrSummary(List<StrikeDataDto> strikes, decimal spotPrice, decimal previousOiPcr = 0)
    {
        if (strikes == null || strikes.Count == 0)
        {
            return new PcrSummaryDto();
        }

        long totalCallOi = 0;
        long totalPutOi = 0;
        long totalCallVolume = 0;
        long totalPutVolume = 0;
        decimal totalCallOiChange = 0;
        decimal totalPutOiChange = 0;

        long atmCallOi = 0;
        long atmPutOi = 0;

        int longBuildup = 0;
        int shortBuildup = 0;
        int shortCovering = 0;
        int longUnwinding = 0;

        // Find ATM strike (closest to spot)
        var atmStrikeObj = strikes.OrderBy(s => Math.Abs(s.StrikePrice - spotPrice)).FirstOrDefault();
        var atmPrice = atmStrikeObj?.StrikePrice ?? spotPrice;

        // Strikes sorted
        var sortedStrikes = strikes.OrderBy(s => s.StrikePrice).ToList();
        var atmIndex = sortedStrikes.FindIndex(s => s.StrikePrice == atmPrice);
        var atmStartIndex = Math.Max(0, atmIndex - 2);
        var atmEndIndex = Math.Min(sortedStrikes.Count - 1, atmIndex + 2);

        for (int i = 0; i < sortedStrikes.Count; i++)
        {
            var strike = sortedStrikes[i];

            if (strike.Call != null)
            {
                totalCallOi += strike.Call.OpenInterest;
                totalCallVolume += strike.Call.Volume;
                totalCallOiChange += strike.Call.OiChange;

                // Buildup for Call
                strike.Call.Buildup = ClassifyBuildup(strike.Call.Change, strike.Call.OiChange);
                CountBuildup(strike.Call.Buildup, ref longBuildup, ref shortBuildup, ref shortCovering, ref longUnwinding);

                if (i >= atmStartIndex && i <= atmEndIndex)
                {
                    atmCallOi += strike.Call.OpenInterest;
                }
            }

            if (strike.Put != null)
            {
                totalPutOi += strike.Put.OpenInterest;
                totalPutVolume += strike.Put.Volume;
                totalPutOiChange += strike.Put.OiChange;

                // Buildup for Put
                strike.Put.Buildup = ClassifyBuildup(strike.Put.Change, strike.Put.OiChange);
                CountBuildup(strike.Put.Buildup, ref longBuildup, ref shortBuildup, ref shortCovering, ref longUnwinding);

                if (i >= atmStartIndex && i <= atmEndIndex)
                {
                    atmPutOi += strike.Put.OpenInterest;
                }
            }

            // Individual strike PCR
            if (strike.Call != null && strike.Call.OpenInterest > 0 && strike.Put != null)
            {
                strike.StrikePcr = Math.Round((decimal)strike.Put.OpenInterest / (decimal)strike.Call.OpenInterest, 2);
            }
        }

        var oiPcr = totalCallOi > 0 ? Math.Round((decimal)totalPutOi / (decimal)totalCallOi, 3) : 1.0m;
        var volumePcr = totalCallVolume > 0 ? Math.Round((decimal)totalPutVolume / (decimal)totalCallVolume, 3) : 1.0m;
        var atmPcr = atmCallOi > 0 ? Math.Round((decimal)atmPutOi / (decimal)atmCallOi, 3) : 1.0m;

        var pcrSlope = previousOiPcr > 0 ? Math.Round(oiPcr - previousOiPcr, 3) : 0m;
        var sentiment = InterpretSentiment(oiPcr);

        return new PcrSummaryDto
        {
            OiPcr = oiPcr,
            VolumePcr = volumePcr,
            AtmPcr = atmPcr,
            PrevOiPcr = previousOiPcr > 0 ? previousOiPcr : oiPcr,
            PcrSlope = pcrSlope,
            Sentiment = sentiment,
            LongBuildupCount = longBuildup,
            ShortBuildupCount = shortBuildup,
            ShortCoveringCount = shortCovering,
            LongUnwindingCount = longUnwinding,
            TotalCallOi = totalCallOi,
            TotalPutOi = totalPutOi,
            CallOiChangeTotal = totalCallOiChange,
            PutOiChangeTotal = totalPutOiChange,
            CalculatedAtUtc = DateTime.UtcNow
        };
    }

    public static BuildupType ClassifyBuildup(decimal priceChange, long oiChange)
    {
        if (priceChange > 0 && oiChange > 0)
            return BuildupType.LongBuildup;
        if (priceChange < 0 && oiChange > 0)
            return BuildupType.ShortBuildup;
        if (priceChange > 0 && oiChange < 0)
            return BuildupType.ShortCovering;
        if (priceChange < 0 && oiChange < 0)
            return BuildupType.LongUnwinding;

        return BuildupType.Neutral;
    }

    private static void CountBuildup(BuildupType type, ref int lb, ref int sb, ref int sc, ref int lu)
    {
        switch (type)
        {
            case BuildupType.LongBuildup: lb++; break;
            case BuildupType.ShortBuildup: sb++; break;
            case BuildupType.ShortCovering: sc++; break;
            case BuildupType.LongUnwinding: lu++; break;
        }
    }

    public static string InterpretSentiment(decimal oiPcr)
    {
        if (oiPcr >= 1.60m)
            return "Extreme Bullish (Overbought Warning)";
        if (oiPcr >= 1.25m)
            return "Strong Bullish (Heavy Put Writing)";
        if (oiPcr >= 1.05m)
            return "Mildly Bullish";
        if (oiPcr >= 0.90m)
            return "Neutral / Rangebound";
        if (oiPcr >= 0.70m)
            return "Mildly Bearish";
        if (oiPcr >= 0.50m)
            return "Strong Bearish (Heavy Call Writing)";

        return "Extreme Bearish (Oversold Bounce Watch)";
    }
}
