using System;
using System.Collections.Generic;
using System.Linq;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Xunit;

namespace AutoFno.Tests.V2;

public class Pillar4SpotTrendTests
{
    private readonly SpotTrendEngine _engine = new();
    private readonly QuantitativeConvictionV2Config _config = new();

    private List<Candle> GenerateCandleSeries(int count, decimal startPrice, decimal stepPerBar)
    {
        var list = new List<Candle>();
        var now = DateTime.UtcNow.AddMinutes(-count);
        decimal p = startPrice;

        for (int i = 0; i < count; i++)
        {
            var open = p;
            p += stepPerBar;
            var close = p;
            var high = Math.Max(open, close) + 2.0m;
            var low = Math.Min(open, close) - 2.0m;

            list.Add(new Candle
            {
                TimestampUtc = now.AddMinutes(i),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = 1000
            });
        }

        return list;
    }

    [Fact]
    public void Bullish_Uptrend_Correctly_Identified()
    {
        // 50 bullish candles trending strongly upwards
        var candles = GenerateCandleSeries(50, 24000m, 10.0m);
        var spot = candles.Last().Close;
        var result = _engine.Analyze(candles, spot, _config);

        Assert.Equal(MarketRegime.StrongBullTrend, result.Regime);
        Assert.True(result.IsSupertrendBullish, "Supertrend should be bullish in strong uptrend");
        Assert.True(result.Ema9 > result.Ema21, "EMA 9 must exceed EMA 21");
        Assert.True(result.TotalSpotPillarScore > 0, "Uptrend should yield positive spot pillar score");
        Assert.InRange(result.TotalSpotPillarScore, -25.0m, 25.0m);
    }

    [Fact]
    public void Bearish_Downtrend_Correctly_Identified()
    {
        // 50 bearish candles trending strongly downwards
        var candles = GenerateCandleSeries(50, 25000m, -10.0m);
        var spot = candles.Last().Close;
        var result = _engine.Analyze(candles, spot, _config);
        Assert.Equal(MarketRegime.StrongBearTrend, result.Regime);
        Assert.False(result.IsSupertrendBullish, "Supertrend should be bearish in strong downtrend");
        Assert.True(result.Ema9 < result.Ema21, "EMA 9 must be below EMA 21");
        Assert.True(result.TotalSpotPillarScore < 0, "Downtrend should yield negative spot pillar score");
        Assert.InRange(result.TotalSpotPillarScore, -25.0m, 25.0m);
    }

    [Fact]
    public void Consolidation_Flat_Market_Identifies_Choppy_Regime()
    {
        // Oscillating flat candles around 24500
        var candles = new List<Candle>();
        var now = DateTime.UtcNow.AddMinutes(-50);
        for (int i = 0; i < 50; i++)
        {
            var offset = (i % 2 == 0) ? 3.0m : -3.0m;
            candles.Add(new Candle
            {
                TimestampUtc = now.AddMinutes(i),
                Open = 24500 + offset,
                High = 24505,
                Low = 24495,
                Close = 24500 - offset,
                Volume = 500
            });
        }

        var spot = candles.Last().Close;
        var result = _engine.Analyze(candles, spot, _config);

        Assert.Equal(MarketRegime.RangeboundChoppy, result.Regime);
    }
}
