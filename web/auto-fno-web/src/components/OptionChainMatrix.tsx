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
    <div className="ob-panel rounded-2xl overflow-hidden shadow-2xl mb-4 border border-[#1A212D] relative">
      {/* Header and Controls */}
      <div className="bg-[#06080C]/90 px-4 py-3 border-b border-[#1A212D] flex flex-wrap items-center justify-between gap-3 relative overflow-hidden">
        {/* Scanner Radar Beam in Option Chain Header */}
        <div className="ob-scanner-beam" />

        <div className="flex items-center space-x-3 relative z-10">
          <h2 className="text-sm font-extrabold text-white font-mono uppercase tracking-wider flex items-center gap-2">
            <span className="ob-grad-brand">REAL-TIME OPTION CHAIN</span>
            <span className="text-xs px-2 py-0.5 rounded bg-[#06080C] border border-cyan-500/40 text-[#45B8FF] shadow-[0_0_10px_rgba(69,184,255,0.2)]">
              {chain.underlyingSymbol} • {chain.expiryDate}
            </span>
          </h2>
        </div>

        {/* Filter Buttons */}
        <div className="flex items-center space-x-2 relative z-10">
          <span className="text-xs text-slate-400 flex items-center gap-1 font-mono">
            <Filter className="w-3.5 h-3.5 text-[#45B8FF]" /> Range:
          </span>
          <div className="bg-[#06080C] p-0.5 rounded-lg border border-[#1A212D] flex items-center shadow-inner">
            {(['5', '10', '15', 'all'] as const).map((r) => (
              <button
                key={r}
                onClick={() => setStrikeFilter(r)}
                className={`px-2.5 py-0.5 rounded text-[11px] font-mono font-bold transition ob-hover-lift ${
                  strikeFilter === r
                    ? 'bg-gradient-to-r from-[#0284C7] to-[#0EA5E9] text-white shadow-[0_0_10px_rgba(14,165,233,0.4)]'
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
          <thead className="bg-[#06080C]/95 sticky top-0 z-20 backdrop-blur-md border-b border-[#1A212D] text-slate-400">
            <tr>
              <th colSpan={7} className="py-2 px-3 text-center text-[#19E58C] font-bold border-r border-[#1A212D] bg-[#19E58C]/[0.06] tracking-wider">
                CALL OPTIONS (CE)
              </th>
              <th className="py-2 px-4 text-center text-[#45B8FF] font-extrabold border-x border-[#1A212D] bg-[#0D1118] tracking-widest">
                STRIKE
              </th>
              <th colSpan={7} className="py-2 px-3 text-center text-[#FF4D67] font-bold border-l border-[#1A212D] bg-[#FF4D67]/[0.06] tracking-wider">
                PUT OPTIONS (PE)
              </th>
            </tr>
            <tr className="text-[10px] text-slate-400 border-b border-[#1A212D] bg-[#06080C]">
              {/* Call Columns */}
              <th className="py-2 px-2 text-center">TRADE</th>
              <th className="py-2 px-2 text-right">DELTA</th>
              <th className="py-2 px-2 text-right">IV%</th>
              <th className="py-2 px-2 text-right">VOLUME</th>
              <th className="py-2 px-2 text-right">OI (CHG%)</th>
              <th className="py-2 px-2 text-center">BUILDUP</th>
              <th className="py-2 px-3 text-right border-r border-[#1A212D] font-bold text-white">LTP</th>

              {/* Center Strike */}
              <th className="py-2 px-4 text-center font-extrabold text-white bg-[#0D1118]">PRICE</th>

              {/* Put Columns */}
              <th className="py-2 px-3 text-left border-l border-[#1A212D] font-bold text-white">LTP</th>
              <th className="py-2 px-2 text-center">BUILDUP</th>
              <th className="py-2 px-2 text-left">OI (CHG%)</th>
              <th className="py-2 px-2 text-left">VOLUME</th>
              <th className="py-2 px-2 text-left">IV%</th>
              <th className="py-2 px-2 text-left">DELTA</th>
              <th className="py-2 px-2 text-center">TRADE</th>
            </tr>
          </thead>

          <tbody className="divide-y divide-[#1A212D]/70 bg-[#06080C]/40">
            {filteredStrikes.map((s) => {
              const isAtm = s.isAtm;
              const isCallItm = spot > s.strikePrice;
              const isPutItm = spot < s.strikePrice;

              const callOiBarWidth = Math.min(100, Math.round(((s.call?.openInterest || 0) / maxCallOi) * 100));
              const putOiBarWidth = Math.min(100, Math.round(((s.put?.openInterest || 0) / maxPutOi) * 100));

              return (
                <tr
                  key={s.strikePrice}
                  className={`transition-colors duration-150 ${
                    isAtm ? 'ob-atm-row' : 'hover:bg-[#11161F]/80'
                  }`}
                >
                  {/* CALLS */}
                  {/* 1. Quick Buy/Sell Call */}
                  <td className={`py-1.5 px-2 text-center ${isCallItm ? 'bg-[#19E58C]/[0.04]' : ''}`}>
                    <div className="flex items-center justify-center space-x-1">
                      <button
                        onClick={() => onQuickOrder(s, 'CE', 'BUY')}
                        className="px-2 py-0.5 ob-btn-bull text-white rounded text-[10px] font-bold shadow-sm"
                        title="Buy Call Option"
                      >
                        B
                      </button>
                      <button
                        onClick={() => onQuickOrder(s, 'CE', 'SELL')}
                        className="px-2 py-0.5 bg-[#1A212D] hover:bg-[#242D3C] text-slate-300 rounded text-[10px] font-bold border border-slate-700/50"
                        title="Sell Call Option"
                      >
                        S
                      </button>
                    </div>
                  </td>

                  {/* 2. Call Delta */}
                  <td className={`py-1.5 px-2 text-right ${isCallItm ? 'bg-[#19E58C]/[0.04]' : ''} text-slate-300`}>
                    {s.call?.greeks?.delta ? s.call.greeks.delta.toFixed(2) : '—'}
                  </td>

                  {/* 3. Call IV */}
                  <td className={`py-1.5 px-2 text-right ${isCallItm ? 'bg-[#19E58C]/[0.04]' : ''} text-slate-400`}>
                    {s.call?.greeks?.iv ? `${s.call.greeks.iv.toFixed(1)}%` : '—'}
                  </td>

                  {/* 4. Call Volume */}
                  <td className={`py-1.5 px-2 text-right ${isCallItm ? 'bg-[#19E58C]/[0.04]' : ''} text-slate-400`}>
                    {s.call?.volume ? (s.call.volume / 1000).toFixed(0) + 'k' : '—'}
                  </td>

                  {/* 5. Call OI with Depth Bar */}
                  <td className={`py-1.5 px-2 text-right ${isCallItm ? 'bg-[#19E58C]/[0.04]' : ''} relative overflow-hidden`}>
                    <div
                      className="absolute right-0 top-0 bottom-0 ob-depth-call z-0"
                      style={{ width: `${callOiBarWidth}%` }}
                    />
                    <div className="relative z-10">
                      <span className="font-bold text-slate-200">
                        {s.call?.openInterest ? (s.call.openInterest / 1000).toFixed(1) + 'k' : '0'}
                      </span>
                      <span
                        className={`text-[9px] ml-1.5 font-bold ${
                          (s.call?.oiChangePercent || 0) >= 0 ? 'text-[#19E58C]' : 'text-[#FF4D67]'
                        }`}
                      >
                        {(s.call?.oiChangePercent || 0) >= 0 ? '+' : ''}
                        {(s.call?.oiChangePercent || 0).toFixed(0)}%
                      </span>
                    </div>
                  </td>

                  {/* 6. Call Buildup Badge */}
                  <td className={`py-1.5 px-2 text-center ${isCallItm ? 'bg-[#19E58C]/[0.04]' : ''}`}>
                    <span
                      className={`text-[9px] px-1.5 py-0.5 rounded font-extrabold tracking-wider ${
                        s.call?.buildup === 'LongBuildup'
                          ? 'ob-buildup-long'
                          : s.call?.buildup === 'ShortBuildup'
                          ? 'ob-buildup-short'
                          : s.call?.buildup === 'ShortCovering'
                          ? 'ob-buildup-cover'
                          : s.call?.buildup === 'LongUnwinding'
                          ? 'ob-buildup-unwind'
                          : 'text-slate-500'
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
                  <td className={`py-1.5 px-3 text-right font-bold border-r border-[#1A212D] ${isCallItm ? 'bg-[#19E58C]/[0.08] text-[#19E58C]' : 'text-white'}`}>
                    ₹{s.call?.ltp ? s.call.ltp.toFixed(2) : '0.00'}
                  </td>

                  {/* CENTER STRIKE */}
                  <td className={`py-2 px-4 text-center font-extrabold border-x border-[#1A212D] bg-[#0D1118] relative ${isAtm ? 'text-[#45B8FF]' : 'text-white'}`}>
                    <div className="flex items-center justify-center space-x-1.5">
                      <span className="tracking-tight text-sm drop-shadow-sm">{s.strikePrice.toLocaleString('en-IN')}</span>
                      {isAtm && (
                        <span className="text-[9px] bg-cyan-500/20 text-[#45B8FF] border border-cyan-400/40 px-1 rounded font-black shadow-[0_0_8px_rgba(69,184,255,0.4)]">
                          ATM
                        </span>
                      )}
                      {s.isMaxPain && (
                        <span className="text-[9px] ob-maxpain-badge px-1 rounded font-black">
                          PAIN
                        </span>
                      )}
                      {s.isCallWall && (
                        <span className="text-[9px] ob-callwall-badge px-1 rounded font-black">
                          RES
                        </span>
                      )}
                      {s.isPutWall && (
                        <span className="text-[9px] ob-putwall-badge px-1 rounded font-black">
                          SUP
                        </span>
                      )}
                    </div>
                  </td>

                  {/* PUTS */}
                  {/* 8. Put LTP */}
                  <td className={`py-1.5 px-3 text-left font-bold border-l border-[#1A212D] ${isPutItm ? 'bg-[#FF4D67]/[0.08] text-[#FF4D67]' : 'text-white'}`}>
                    ₹{s.put?.ltp ? s.put.ltp.toFixed(2) : '0.00'}
                  </td>

                  {/* 9. Put Buildup Badge */}
                  <td className={`py-1.5 px-2 text-center ${isPutItm ? 'bg-[#FF4D67]/[0.04]' : ''}`}>
                    <span
                      className={`text-[9px] px-1.5 py-0.5 rounded font-extrabold tracking-wider ${
                        s.put?.buildup === 'LongBuildup'
                          ? 'ob-buildup-short'
                          : s.put?.buildup === 'ShortBuildup'
                          ? 'ob-buildup-long'
                          : s.put?.buildup === 'ShortCovering'
                          ? 'ob-buildup-unwind'
                          : s.put?.buildup === 'LongUnwinding'
                          ? 'ob-buildup-cover'
                          : 'text-slate-500'
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

                  {/* 10. Put OI with Depth Bar */}
                  <td className={`py-1.5 px-2 text-left ${isPutItm ? 'bg-[#FF4D67]/[0.04]' : ''} relative overflow-hidden`}>
                    <div
                      className="absolute left-0 top-0 bottom-0 ob-depth-put z-0"
                      style={{ width: `${putOiBarWidth}%` }}
                    />
                    <div className="relative z-10">
                      <span className="font-bold text-slate-200">
                        {s.put?.openInterest ? (s.put.openInterest / 1000).toFixed(1) + 'k' : '0'}
                      </span>
                      <span
                        className={`text-[9px] ml-1.5 font-bold ${
                          (s.put?.oiChangePercent || 0) >= 0 ? 'text-[#19E58C]' : 'text-[#FF4D67]'
                        }`}
                      >
                        {(s.put?.oiChangePercent || 0) >= 0 ? '+' : ''}
                        {(s.put?.oiChangePercent || 0).toFixed(0)}%
                      </span>
                    </div>
                  </td>

                  {/* 11. Put Volume */}
                  <td className={`py-1.5 px-2 text-left ${isPutItm ? 'bg-[#FF4D67]/[0.04]' : ''} text-slate-400`}>
                    {s.put?.volume ? (s.put.volume / 1000).toFixed(0) + 'k' : '—'}
                  </td>

                  {/* 12. Put IV */}
                  <td className={`py-1.5 px-2 text-left ${isPutItm ? 'bg-[#FF4D67]/[0.04]' : ''} text-slate-400`}>
                    {s.put?.greeks?.iv ? `${s.put.greeks.iv.toFixed(1)}%` : '—'}
                  </td>

                  {/* 13. Put Delta */}
                  <td className={`py-1.5 px-2 text-left ${isPutItm ? 'bg-[#FF4D67]/[0.04]' : ''} text-slate-300`}>
                    {s.put?.greeks?.delta ? s.put.greeks.delta.toFixed(2) : '—'}
                  </td>

                  {/* 14. Quick Buy/Sell Put */}
                  <td className={`py-1.5 px-2 text-center ${isPutItm ? 'bg-[#FF4D67]/[0.04]' : ''}`}>
                    <div className="flex items-center justify-center space-x-1">
                      <button
                        onClick={() => onQuickOrder(s, 'PE', 'BUY')}
                        className="px-2 py-0.5 ob-btn-bear text-white rounded text-[10px] font-bold shadow-sm"
                        title="Buy Put Option"
                      >
                        B
                      </button>
                      <button
                        onClick={() => onQuickOrder(s, 'PE', 'SELL')}
                        className="px-2 py-0.5 bg-[#1A212D] hover:bg-[#242D3C] text-slate-300 rounded text-[10px] font-bold border border-slate-700/50"
                        title="Sell Put Option"
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
