import {
  CandlestickSeries,
  ColorType,
  createChart,
  CrosshairMode,
  HistogramSeries,
  LineSeries,
  type IChartApi,
} from 'lightweight-charts';
import { BarChart2, RefreshCw } from 'lucide-react';
import React, { useEffect, useRef, useState } from 'react';
import { fnoApi } from '../api/fnoApi';
import type { Candle, IndexSymbol } from '../types/fnoTrading';

interface TradingViewChartProps {
  indexSymbol: IndexSymbol;
  spotPrice: number;
}

export const TradingViewChart: React.FC<TradingViewChartProps> = ({
  indexSymbol,
  spotPrice,
}) => {
  const chartContainerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const candleSeriesRef = useRef<any>(null);
  const ema9SeriesRef = useRef<any>(null);
  const ema21SeriesRef = useRef<any>(null);
  const volumeSeriesRef = useRef<any>(null);
  const lastBarRef = useRef<{ time: number; open: number; high: number; low: number; close: number } | null>(null);

  const [interval, setInterval] = useState<'1minute' | '5minute'>('1minute');
  const [loading, setLoading] = useState(false);

  const loadCandles = async () => {
    try {
      setLoading(true);
      const data = await fnoApi.getCandles(indexSymbol, interval);
      if (!candleSeriesRef.current || data.length === 0) return;

      const candleData: any[] = [];
      const volumeData: any[] = [];
      const closePrices: { time: number; close: number }[] = [];

      data.forEach((c: Candle) => {
        const timeInSeconds = Math.floor(new Date(c.timestampUtc).getTime() / 1000);
        candleData.push({
          time: timeInSeconds,
          open: c.open,
          high: c.high,
          low: c.low,
          close: c.close,
        });

        volumeData.push({
          time: timeInSeconds,
          value: c.volume,
          color: c.close >= c.open ? 'rgba(16, 185, 129, 0.4)' : 'rgba(239, 68, 68, 0.4)',
        });

        closePrices.push({ time: timeInSeconds, close: c.close });
      });

      if (candleData.length > 0) {
        lastBarRef.current = { ...candleData[candleData.length - 1] };
      }

      candleSeriesRef.current.setData(candleData);
      volumeSeriesRef.current?.setData(volumeData);

      // Compute EMA 9 and EMA 21
      if (closePrices.length >= 9) {
        const ema9Data = calculateEma(closePrices, 9);
        ema9SeriesRef.current?.setData(ema9Data);
      }
      if (closePrices.length >= 21) {
        const ema21Data = calculateEma(closePrices, 21);
        ema21SeriesRef.current?.setData(ema21Data);
      }

      chartRef.current?.timeScale().fitContent();
    } catch (err) {
      console.error('Failed loading candles:', err);
    } finally {
      setLoading(false);
    }
  };

  // Initialize Chart
  useEffect(() => {
    if (!chartContainerRef.current) return;

    chartContainerRef.current.innerHTML = '';

    const chart = createChart(chartContainerRef.current, {
      layout: {
        background: { type: ColorType.Solid, color: '#06080C' },
        textColor: '#7B8B9E',
      },
      grid: {
        vertLines: { color: 'rgba(26, 33, 45, 0.45)' },
        horzLines: { color: 'rgba(26, 33, 45, 0.45)' },
      },
      crosshair: {
        mode: CrosshairMode.Normal,
      },
      rightPriceScale: {
        borderColor: '#1A212D',
        scaleMargins: {
          top: 0.1,
          bottom: 0.25,
        },
      },
      timeScale: {
        borderColor: '#1A212D',
        timeVisible: true,
        secondsVisible: false,
      },
      width: chartContainerRef.current.clientWidth,
      height: 380,
    });

    chartRef.current = chart;

    // Add Candlestick Series (lightweight-charts v5 API)
    const candleSeries = chart.addSeries(CandlestickSeries, {
      upColor: '#19E58C',
      downColor: '#FF4D67',
      borderVisible: false,
      wickUpColor: '#19E58C',
      wickDownColor: '#FF4D67',
    });
    candleSeriesRef.current = candleSeries;

    // Add EMA 9
    const ema9Series = chart.addSeries(LineSeries, {
      color: '#45B8FF', // OrderBlock Sky Cyan
      lineWidth: 1,
      title: 'EMA 9',
    });
    ema9SeriesRef.current = ema9Series;

    // Add EMA 21
    const ema21Series = chart.addSeries(LineSeries, {
      color: '#FFB020', // OrderBlock Amber
      lineWidth: 1,
      title: 'EMA 21',
    });
    ema21SeriesRef.current = ema21Series;

    // Add Volume Series
    const volumeSeries = chart.addSeries(HistogramSeries, {
      priceFormat: {
        type: 'volume',
      },
      priceScaleId: '', // Overlay on separate internal scale
    });
    volumeSeries.priceScale().applyOptions({
      scaleMargins: {
        top: 0.8,
        bottom: 0,
      },
    });
    volumeSeriesRef.current = volumeSeries;

    // Resize Observer
    const handleResize = () => {
      if (chartContainerRef.current) {
        chart.applyOptions({ width: chartContainerRef.current.clientWidth });
      }
    };
    window.addEventListener('resize', handleResize);

    loadCandles();

    return () => {
      window.removeEventListener('resize', handleResize);
      chart.remove();
    };
  }, [indexSymbol, interval]);

  // Update current bar with live spot tick
  useEffect(() => {
    if (!candleSeriesRef.current || spotPrice <= 0 || !lastBarRef.current) return;
    try {
      const last = lastBarRef.current;
      const updatedBar = {
        time: last.time,
        open: last.open,
        high: Math.max(last.high, spotPrice),
        low: Math.min(last.low, spotPrice),
        close: spotPrice,
      };
      lastBarRef.current = updatedBar;
      candleSeriesRef.current.update(updatedBar);
    } catch {
      // Ignore timestamp sequencing micro-errors
    }
  }, [spotPrice]);

  return (
    <div className="ob-panel rounded-2xl overflow-hidden shadow-2xl mb-4 border border-[#1A212D] relative">
      {/* Chart Top Controls */}
      <div className="bg-[#06080C]/90 px-4 py-2.5 border-b border-[#1A212D] flex items-center justify-between relative overflow-hidden">
        {/* Scanner Radar Beam in Chart Header */}
        <div className="ob-scanner-beam" />

        <div className="flex items-center space-x-3 relative z-10">
          <div className="flex items-center space-x-2 text-xs font-bold text-white font-mono uppercase">
            <BarChart2 className="w-4 h-4 text-[#45B8FF] animate-pulse" />
            <span className="ob-grad-brand">{indexSymbol} SPOT INTRADAY</span>
          </div>
          <div className="flex items-center space-x-3 text-[10px] font-mono">
            <span className="flex items-center gap-1.5 text-[#45B8FF] bg-[#45B8FF]/10 px-1.5 py-0.5 rounded border border-[#45B8FF]/30">
              <span className="w-2 h-0.5 bg-[#45B8FF] rounded" /> EMA 9
            </span>
            <span className="flex items-center gap-1.5 text-[#FFB020] bg-[#FFB020]/10 px-1.5 py-0.5 rounded border border-[#FFB020]/30">
              <span className="w-2 h-0.5 bg-[#FFB020] rounded" /> EMA 21
            </span>
          </div>
        </div>

        <div className="flex items-center space-x-2 relative z-10">
          {/* Interval Switcher */}
          <div className="bg-[#06080C] p-0.5 rounded-lg border border-[#1A212D] flex shadow-inner">
            {(['1minute', '5minute'] as const).map((int) => (
              <button
                key={int}
                onClick={() => setInterval(int)}
                className={`px-2.5 py-0.5 text-[10px] font-mono font-bold rounded transition ob-hover-lift ${
                  interval === int
                    ? 'bg-gradient-to-r from-[#0284C7] to-[#0EA5E9] text-white shadow-[0_0_10px_rgba(14,165,233,0.4)]'
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                {int === '1minute' ? '1m' : '5m'}
              </button>
            ))}
          </div>

          <button
            onClick={loadCandles}
            disabled={loading}
            className="p-1.5 bg-[#11161F] hover:bg-[#161C27] text-slate-400 hover:text-white rounded-lg border border-[#1A212D] transition ob-hover-lift"
            title="Reload Chart Candles"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-[#45B8FF]' : ''}`} />
          </button>
        </div>
      </div>

      {/* Chart Canvas */}
      <div ref={chartContainerRef} className="w-full relative" />
    </div>
  );
};

// Helper EMA calculation
function calculateEma(prices: { time: number; close: number }[], period: number) {
  const k = 2 / (period + 1);
  const result: { time: number; value: number }[] = [];

  let sum = 0;
  for (let i = 0; i < period; i++) {
    sum += prices[i].close;
  }
  let ema = sum / period;
  result.push({ time: prices[period - 1].time, value: Math.round(ema * 100) / 100 });

  for (let i = period; i < prices.length; i++) {
    ema = prices[i].close * k + ema * (1 - k);
    result.push({ time: prices[i].time, value: Math.round(ema * 100) / 100 });
  }

  return result;
}
