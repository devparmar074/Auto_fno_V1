using AutoFno.Domain.Models;

namespace AutoFno.Application.Services.Mathematics;

public class TechnicalIndicators
{
    public decimal Ema9 { get; set; }
    public decimal Ema21 { get; set; }
    public decimal Ema50 { get; set; }
    public decimal Rsi14 { get; set; }
    public decimal Vwap { get; set; }
    public decimal Supertrend { get; set; }
    public bool IsSupertrendBullish { get; set; }
    public decimal VolumeSma20 { get; set; }
    public decimal VolumeRatio { get; set; }
}

public static class SpotIndicatorCalculator
{
    public static TechnicalIndicators Calculate(List<Candle> candles)
    {
        var result = new TechnicalIndicators();
        if (candles == null || candles.Count == 0)
        {
            return result;
        }

        // Sort ascending by time
        var sorted = candles.OrderBy(c => c.TimestampUtc).ToList();
        var closes = sorted.Select(c => (double)c.Close).ToArray();

        result.Ema9 = (decimal)CalculateEma(closes, 9);
        result.Ema21 = (decimal)CalculateEma(closes, 21);
        result.Ema50 = (decimal)CalculateEma(closes, 50);
        result.Rsi14 = (decimal)CalculateRsi(closes, 14);
        result.Vwap = (decimal)CalculateVwap(sorted);

        var (stValue, stBullish) = CalculateSupertrend(sorted, 10, 3.0);
        result.Supertrend = (decimal)stValue;
        result.IsSupertrendBullish = stBullish;

        // Volume SMA 20
        if (sorted.Count >= 20)
        {
            var vol20 = sorted.TakeLast(20).Select(c => (double)c.Volume).Average();
            result.VolumeSma20 = (decimal)vol20;
            var currentVol = (double)sorted.Last().Volume;
            result.VolumeRatio = vol20 > 0 ? (decimal)Math.Round(currentVol / vol20, 2) : 1m;
        }
        else
        {
            result.VolumeRatio = 1m;
        }

        return result;
    }

    public static double CalculateEma(double[] values, int period)
    {
        if (values.Length == 0) return 0;
        if (values.Length < period) period = values.Length;

        double multiplier = 2.0 / (period + 1);
        double ema = values.Take(period).Average();

        for (int i = period; i < values.Length; i++)
        {
            ema = ((values[i] - ema) * multiplier) + ema;
        }

        return Math.Round(ema, 2);
    }

    public static double CalculateRsi(double[] values, int period = 14)
    {
        if (values.Length <= period) return 50.0;

        double gainSum = 0;
        double lossSum = 0;

        for (int i = 1; i <= period; i++)
        {
            var diff = values[i] - values[i - 1];
            if (diff >= 0) gainSum += diff;
            else lossSum += -diff;
        }

        double avgGain = gainSum / period;
        double avgLoss = lossSum / period;

        for (int i = period + 1; i < values.Length; i++)
        {
            var diff = values[i] - values[i - 1];
            if (diff >= 0)
            {
                avgGain = (avgGain * (period - 1) + diff) / period;
                avgLoss = (avgLoss * (period - 1)) / period;
            }
            else
            {
                avgGain = (avgGain * (period - 1)) / period;
                avgLoss = (avgLoss * (period - 1) + (-diff)) / period;
            }
        }

        if (avgLoss == 0) return 100.0;
        double rs = avgGain / avgLoss;
        return Math.Round(100.0 - (100.0 / (1.0 + rs)), 2);
    }

    public static double CalculateVwap(List<Candle> candles)
    {
        if (candles.Count == 0) return 0;

        double cumulativePv = 0;
        long cumulativeVolume = 0;

        foreach (var c in candles)
        {
            double typicalPrice = (double)(c.High + c.Low + c.Close) / 3.0;
            cumulativePv += typicalPrice * c.Volume;
            cumulativeVolume += c.Volume;
        }

        if (cumulativeVolume == 0) return (double)candles.Last().Close;
        return Math.Round(cumulativePv / cumulativeVolume, 2);
    }

    public static (double Supertrend, bool IsBullish) CalculateSupertrend(List<Candle> candles, int period = 10, double multiplier = 3.0)
    {
        if (candles.Count < period)
        {
            var last = (double)(candles.LastOrDefault()?.Close ?? 0m);
            return (last, true);
        }

        int count = candles.Count;
        double[] tr = new double[count];
        tr[0] = (double)(candles[0].High - candles[0].Low);

        for (int i = 1; i < count; i++)
        {
            double hl = (double)(candles[i].High - candles[i].Low);
            double hc = Math.Abs((double)(candles[i].High - candles[i - 1].Close));
            double lc = Math.Abs((double)(candles[i].Low - candles[i - 1].Close));
            tr[i] = Math.Max(hl, Math.Max(hc, lc));
        }

        // Wilder's ATR
        double atr = tr.Take(period).Average();
        double[] upperBand = new double[count];
        double[] lowerBand = new double[count];
        double[] supertrend = new double[count];
        bool[] inUptrend = new bool[count];

        for (int i = 0; i < count; i++)
        {
            if (i >= period)
            {
                atr = (atr * (period - 1) + tr[i]) / period;
            }

            double hl2 = (double)(candles[i].High + candles[i].Low) / 2.0;
            upperBand[i] = hl2 + (multiplier * atr);
            lowerBand[i] = hl2 - (multiplier * atr);

            if (i > 0)
            {
                if (lowerBand[i] < lowerBand[i - 1] || (double)candles[i - 1].Close < lowerBand[i - 1])
                    lowerBand[i] = lowerBand[i];
                else
                    lowerBand[i] = Math.Max(lowerBand[i], lowerBand[i - 1]);

                if (upperBand[i] > upperBand[i - 1] || (double)candles[i - 1].Close > upperBand[i - 1])
                    upperBand[i] = upperBand[i];
                else
                    upperBand[i] = Math.Min(upperBand[i], upperBand[i - 1]);

                double prevClose = (double)candles[i - 1].Close;
                double currClose = (double)candles[i].Close;

                if (inUptrend[i - 1])
                {
                    if (currClose < lowerBand[i])
                    {
                        inUptrend[i] = false;
                        supertrend[i] = upperBand[i];
                    }
                    else
                    {
                        inUptrend[i] = true;
                        supertrend[i] = lowerBand[i];
                    }
                }
                else
                {
                    if (currClose > upperBand[i])
                    {
                        inUptrend[i] = true;
                        supertrend[i] = lowerBand[i];
                    }
                    else
                    {
                        inUptrend[i] = false;
                        supertrend[i] = upperBand[i];
                    }
                }
            }
            else
            {
                inUptrend[i] = true;
                supertrend[i] = lowerBand[i];
            }
        }

        return (Math.Round(supertrend.Last(), 2), inUptrend.Last());
    }
}
