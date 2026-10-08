import { ArrowDown, ArrowUp, ChevronDown, Filter, Zap } from 'lucide-react';
import React, { useState } from 'react';
import type { OptionChainDto, OptionContractDetailDto, OptionType, StrikeDataDto } from '../types/fnoTrading';

interface OptionChainMatrixProps {
  chain: OptionChainDto;
  onQuickOrder: (strike: StrikeDataDto, type: OptionType, action: 'BUY' | 'SELL') => void;
}

export const OptionChainMatrix: React.FC<OptionChainMatrixProps> = ({
  chain,
  onQuickOrder,
}) => {
  const [strikeFilter, setStrikeFilter] = useState<'5' | '10' | '15' | 'all'>('10');

  const spot = chain.spotPrice || 0;
  const strikes = chain.strikes || [];

  // Filter strikes around ATM
  const filteredStrikes = React.useMemo(() => {
    if (strikeFilter === 'all' || strikes.length === 0) return strikes;

    const count = parseInt(strikeFilter, 10);
    const atmIndex = strikes.findIndex((s) => s.isAtm) !== -1
      ? strikes.findIndex((s) => s.isAtm)
      : Math.floor(strikes.length / 2);

    const start = Math.max(0, atmIndex - count);
    const end = Math.min(strikes.length, atmIndex + count + 1);

    return strikes.slice(start, end);
  }, [strikes, strikeFilter]);

  // Max OI for relative bar scale
  const maxCallOi = Math.max(...strikes.map((s) => s.call?.openInterest || 0), 1);
  const maxPutOi = Math.max(...strikes.map((s) => s.put?.openInterest || 0), 1);

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-2xl mb-4">
      {/* Header and Controls */}
      <div className="bg-slate-950 px-4 py-3 border-b border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center space-x-3">
          <h2 className="text-sm font-extrabold text-white font-mono uppercase tracking-wider flex items-center gap-2">
            <span>REAL-TIME OPTION CHAIN</span>
            <span className="text-xs px-2 py-0.5 rounded bg-cyan-950 border border-cyan-500/30 text-cyan-400">
              {chain.underlyingSymbol} {chain.expiryDate}
            </span>
          </h2>
        </div>

        {/* Filter Buttons */}
        <div className="flex items-center space-x-2">
          <span className="text-xs text-slate-400 flex items-center gap-1">
            <Filter className="w-3.5 h-3.5" /> Range:
          </span>
          <div className="bg-slate-900 p-0.5 rounded-lg border border-slate-800 flex items-center">
            {(['5', '10', '15', 'all'] as const).map((r) => (
              <button
                key={r}
                onClick={() => setStrikeFilter(r)}
                className={`px-2.5 py-0.5 rounded text-[11px] font-mono font-bold transition ${
                  strikeFilter === r
                    ? 'bg-cyan-600 text-white'
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                {r === 'all' ? 'ALL' : `±${r}`}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Option Chain Table */}
      <div className="overflow-x-auto max-h-[580px] overflow-y-auto">
        <table className="w-full border-collapse text-xs font-mono text-left select-none">
          {/* Main Column Group Headers */}
          <thead className="bg-slate-950/90 sticky top-0 z-20 backdrop-blur-sm border-b border-slate-800 text-slate-400">
            <tr>
              <th colSpan={7} className="py-1.5 px-3 text-center text-emerald-400 font-bold border-r border-slate-800 bg-emerald-950/20">
                CALL OPTIONS (CE)
              </th>
              <th className="py-1.5 px-4 text-center text-cyan-400 font-extrabold border-x border-slate-800 bg-slate-900">
                STRIKE
              </th>
              <th colSpan={7} className="py-1.5 px-3 text-center text-rose-400 font-bold border-l border-slate-800 bg-rose-950/20">
                PUT OPTIONS (PE)
              </th>
            </tr>
            <tr className="text-[10px] text-slate-400 border-b border-slate-800 bg-slate-950">
              {/* Call Columns */}
              <th className="py-2 px-2 text-center">TRADE</th>
              <th className="py-2 px-2 text-right">DELTA</th>
              <th className="py-2 px-2 text-right">IV%</th>
              <th className="py-2 px-2 text-right">VOLUME</th>
              <th className="py-2 px-2 text-right">OI (CHG%)</th>
              <th className="py-2 px-2 text-center">BUILDUP</th>
              <th className="py-2 px-3 text-right border-r border-slate-800 font-bold text-white">LTP</th>

              {/* Center Strike */}
              <th className="py-2 px-4 text-center font-extrabold text-white bg-slate-900">PRICE</th>

              {/* Put Columns */}
              <th className="py-2 px-3 text-left border-l border-slate-800 font-bold text-white">LTP</th>
              <th className="py-2 px-2 text-center">BUILDUP</th>
              <th className="py-2 px-2 text-left">OI (CHG%)</th>
              <th className="py-2 px-2 text-left">VOLUME</th>
              <th className="py-2 px-2 text-left">IV%</th>
              <th className="py-2 px-2 text-left">DELTA</th>
              <th className="py-2 px-2 text-center">TRADE</th>
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-800/60">
            {filteredStrikes.map((s) => {
              const isAtm = s.isAtm;
              const isCallItm = spot > s.strikePrice;
              const isPutItm = spot < s.strikePrice;

              const callOiBarWidth = Math.min(100, Math.round(((s.call?.openInterest || 0) / maxCallOi) * 100));
              const putOiBarWidth = Math.min(100, Math.round(((s.put?.openInterest || 0) / maxPutOi) * 100));

              return (
                <tr
                  key={s.strikePrice}
                  className={`hover:bg-slate-800/40 transition-colors ${
                    isAtm ? 'bg-cyan-950/30 border-y-2 border-cyan-500/60 font-semibold' : ''
                  }`}
                >
                  {/* CALLS */}
                  {/* 1. Quick Buy/Sell Call */}
                  <td className={`py-1 px-2 text-center ${isCallItm ? 'bg-emerald-950/10' : ''}`}>
                    <div className="flex items-center justify-center space-x-1">
                      <button
                        onClick={() => onQuickOrder(s, 'CE', 'BUY')}
                        className="px-1.5 py-0.5 bg-emerald-600/80 hover:bg-emerald-500 text-white rounded text-[10px] font-bold"
                        title="Buy Call"
                      >
                        B
                      </button>
                      <button
                        onClick={() => onQuickOrder(s, 'CE', 'SELL')}
                        className="px-1.5 py-0.5 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded text-[10px] font-bold"
                        title="Sell Call"
                      >
                        S
                      </button>
                    </div>
                  </td>

                  {/* 2. Call Delta */}
                  <td className={`py-1 px-2 text-right ${isCallItm ? 'bg-emerald-950/10' : ''} text-slate-300`}>
                    {s.call?.greeks?.delta ? s.call.greeks.delta.toFixed(2) : '—'}
                  </td>

                  {/* 3. Call IV */}
                  <td className={`py-1 px-2 text-right ${isCallItm ? 'bg-emerald-950/10' : ''} text-slate-400`}>
                    {s.call?.greeks?.iv ? `${s.call.greeks.iv.toFixed(1)}%` : '—'}
                  </td>

                  {/* 4. Call Volume */}
                  <td className={`py-1 px-2 text-right ${isCallItm ? 'bg-emerald-950/10' : ''} text-slate-400`}>
                    {s.call?.volume ? (s.call.volume / 1000).toFixed(0) + 'k' : '—'}
                  </td>

                  {/* 5. Call OI with bar */}
                  <td className={`py-1 px-2 text-right ${isCallItm ? 'bg-emerald-950/10' : ''} relative`}>
                    <div
                      className="absolute right-0 top-0 bottom-0 bg-emerald-500/10 z-0"
                      style={{ width: `${callOiBarWidth}%` }}
                    />
                    <div className="relative z-10">
                      <span className="font-bold text-slate-200">
                        {s.call?.openInterest ? (s.call.openInterest / 1000).toFixed(1) + 'k' : '0'}
                      </span>
                      <span
                        className={`text-[9px] ml-1 ${
                          (s.call?.oiChangePercent || 0) >= 0 ? 'text-emerald-400' : 'text-rose-400'
                        }`}
                      >
                        {(s.call?.oiChangePercent || 0) >= 0 ? '+' : ''}
                        {(s.call?.oiChangePercent || 0).toFixed(0)}%
                      </span>
                    </div>
                  </td>

                  {/* 6. Call Buildup Badge */}
                  <td className={`py-1 px-2 text-center ${isCallItm ? 'bg-emerald-950/10' : ''}`}>
                    <span
                      className={`text-[9px] px-1 py-0.5 rounded font-bold ${
                        s.call?.buildup === 'LongBuildup'
                          ? 'bg-emerald-500/20 text-emerald-400'
                          : s.call?.buildup === 'ShortBuildup'
                          ? 'bg-rose-500/20 text-rose-400'
                          : s.call?.buildup === 'ShortCovering'
                          ? 'bg-cyan-500/20 text-cyan-400'
                          : s.call?.buildup === 'LongUnwinding'
                          ? 'bg-amber-500/20 text-amber-400'
                          : 'text-slate-400'
                      }`}
                    >
                      {s.call?.buildup === 'LongBuildup'
                        ? 'LB'
                        : s.call?.buildup === 'ShortBuildup'
                        ? 'SB'
                        : s.call?.buildup === 'ShortCovering'
                        ? 'SC'
                        : s.call?.buildup === 'LongUnwinding'
                        ? 'LU'
                        : '—'}
                    </span>
                  </td>

                  {/* 7. Call LTP */}
                  <td className={`py-1 px-3 text-right font-bold border-r border-slate-800 ${isCallItm ? 'bg-emerald-950/20 text-emerald-300' : 'text-white'}`}>
                    ₹{s.call?.ltp ? s.call.ltp.toFixed(2) : '0.00'}
                  </td>

                  {/* CENTER STRIKE */}
                  <td className={`py-1.5 px-4 text-center font-extrabold border-x border-slate-800 bg-slate-950 relative ${isAtm ? 'text-cyan-400' : 'text-white'}`}>
                    <div className="flex items-center justify-center space-x-1.5">
                      <span>{s.strikePrice.toLocaleString('en-IN')}</span>
                      {isAtm && (
                        <span className="text-[9px] bg-cyan-500/30 text-cyan-300 px-1 rounded font-bold">ATM</span>
                      )}
                      {s.isMaxPain && (
                        <span className="text-[9px] bg-amber-500/30 text-amber-300 px-1 rounded font-bold">PAIN</span>
                      )}
                      {s.isCallWall && (
                        <span className="text-[9px] bg-rose-500/30 text-rose-300 px-1 rounded font-bold">RES</span>
                      )}
                      {s.isPutWall && (
                        <span className="text-[9px] bg-emerald-500/30 text-emerald-300 px-1 rounded font-bold">SUP</span>
                      )}
                    </div>
                  </td>

                  {/* PUTS */}
                  {/* 8. Put LTP */}
                  <td className={`py-1 px-3 text-left font-bold border-l border-slate-800 ${isPutItm ? 'bg-rose-950/20 text-rose-300' : 'text-white'}`}>
                    ₹{s.put?.ltp ? s.put.ltp.toFixed(2) : '0.00'}
                  </td>

                  {/* 9. Put Buildup Badge */}
                  <td className={`py-1 px-2 text-center ${isPutItm ? 'bg-rose-950/10' : ''}`}>
                    <span
                      className={`text-[9px] px-1 py-0.5 rounded font-bold ${
                        s.put?.buildup === 'LongBuildup'
                          ? 'bg-rose-500/20 text-rose-400'
                          : s.put?.buildup === 'ShortBuildup'
                          ? 'bg-emerald-500/20 text-emerald-400'
                          : s.put?.buildup === 'ShortCovering'
                          ? 'bg-amber-500/20 text-amber-400'
                          : s.put?.buildup === 'LongUnwinding'
                          ? 'bg-cyan-500/20 text-cyan-400'
                          : 'text-slate-400'
                      }`}
                    >
                      {s.put?.buildup === 'LongBuildup'
                        ? 'LB'
                        : s.put?.buildup === 'ShortBuildup'
                        ? 'SB'
                        : s.put?.buildup === 'ShortCovering'
                        ? 'SC'
                        : s.put?.buildup === 'LongUnwinding'
                        ? 'LU'
                        : '—'}
                    </span>
                  </td>

                  {/* 10. Put OI with bar */}
                  <td className={`py-1 px-2 text-left ${isPutItm ? 'bg-rose-950/10' : ''} relative`}>
                    <div
                      className="absolute left-0 top-0 bottom-0 bg-rose-500/10 z-0"
                      style={{ width: `${putOiBarWidth}%` }}
                    />
                    <div className="relative z-10">
                      <span className="font-bold text-slate-200">
                        {s.put?.openInterest ? (s.put.openInterest / 1000).toFixed(1) + 'k' : '0'}
                      </span>
                      <span
                        className={`text-[9px] ml-1 ${
                          (s.put?.oiChangePercent || 0) >= 0 ? 'text-emerald-400' : 'text-rose-400'
                        }`}
                      >
                        {(s.put?.oiChangePercent || 0) >= 0 ? '+' : ''}
                        {(s.put?.oiChangePercent || 0).toFixed(0)}%
                      </span>
                    </div>
                  </td>

                  {/* 11. Put Volume */}
                  <td className={`py-1 px-2 text-left ${isPutItm ? 'bg-rose-950/10' : ''} text-slate-400`}>
                    {s.put?.volume ? (s.put.volume / 1000).toFixed(0) + 'k' : '—'}
                  </td>

                  {/* 12. Put IV */}
                  <td className={`py-1 px-2 text-left ${isPutItm ? 'bg-rose-950/10' : ''} text-slate-400`}>
                    {s.put?.greeks?.iv ? `${s.put.greeks.iv.toFixed(1)}%` : '—'}
                  </td>

                  {/* 13. Put Delta */}
                  <td className={`py-1 px-2 text-left ${isPutItm ? 'bg-rose-950/10' : ''} text-slate-300`}>
                    {s.put?.greeks?.delta ? s.put.greeks.delta.toFixed(2) : '—'}
                  </td>

                  {/* 14. Quick Buy/Sell Put */}
                  <td className={`py-1 px-2 text-center ${isPutItm ? 'bg-rose-950/10' : ''}`}>
                    <div className="flex items-center justify-center space-x-1">
                      <button
                        onClick={() => onQuickOrder(s, 'PE', 'BUY')}
                        className="px-1.5 py-0.5 bg-rose-600/80 hover:bg-rose-500 text-white rounded text-[10px] font-bold"
                        title="Buy Put"
                      >
                        B
                      </button>
                      <button
                        onClick={() => onQuickOrder(s, 'PE', 'SELL')}
                        className="px-1.5 py-0.5 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded text-[10px] font-bold"
                        title="Sell Put"
                      >
                        S
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};
