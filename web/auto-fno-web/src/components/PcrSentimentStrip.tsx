import { ArrowDownRight, ArrowUpRight, BarChart3, Compass, Shield, Target } from 'lucide-react';
import React from 'react';
import type { MaxPainDto, PcrSummaryDto } from '../types/fnoTrading';

interface PcrSentimentStripProps {
  pcr: PcrSummaryDto;
  maxPain: MaxPainDto;
  callWall: number;
  putWall: number;
  spotPrice: number;
}

export const PcrSentimentStrip: React.FC<PcrSentimentStripProps> = ({
  pcr,
  maxPain,
  callWall,
  putWall,
  spotPrice,
}) => {
  const oiPcr = pcr.oiPcr || 1.0;
  const isPcrBullish = oiPcr >= 1.05;
  const isPcrBearish = oiPcr <= 0.90;

  const totalOi = (pcr.totalCallOi || 0) + (pcr.totalPutOi || 0);
  const callOiPercent = totalOi > 0 ? ((pcr.totalCallOi || 0) / totalOi) * 100 : 50;
  const putOiPercent = totalOi > 0 ? ((pcr.totalPutOi || 0) / totalOi) * 100 : 50;

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3 my-3">
      {/* 1. PCR Meter */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-xl p-3.5 flex flex-col justify-between">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-400">
            <Compass className="w-3.5 h-3.5 text-cyan-400" />
            <span>Put-Call Ratio (PCR)</span>
          </div>
          <span
            className={`text-[10px] font-bold px-2 py-0.5 rounded uppercase font-mono ${
              isPcrBullish
                ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                : isPcrBearish
                ? 'bg-rose-500/10 text-rose-400 border border-rose-500/20'
                : 'bg-slate-800 text-slate-300'
            }`}
          >
            {pcr.sentiment || 'Neutral'}
          </span>
        </div>

        <div className="flex items-baseline justify-between mb-2">
          <div>
            <div className="text-2xl font-black font-mono text-white tracking-tight">
              {oiPcr.toFixed(2)}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">Open Interest PCR</div>
          </div>
          <div className="text-right">
            <div className="text-sm font-bold font-mono text-slate-200">
              {pcr.volumePcr ? pcr.volumePcr.toFixed(2) : '1.00'}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">Volume PCR</div>
          </div>
          <div className="text-right">
            <div className="text-sm font-bold font-mono text-slate-200">
              {pcr.atmPcr ? pcr.atmPcr.toFixed(2) : '1.00'}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">ATM ±2 PCR</div>
          </div>
        </div>

        {/* Visual Mini Slider */}
        <div className="w-full bg-slate-950 rounded-full h-1.5 overflow-hidden flex border border-slate-800">
          <div
            className="bg-gradient-to-r from-rose-500 via-amber-400 to-emerald-500 h-full transition-all duration-500"
            style={{ width: `${Math.min(100, Math.max(0, (oiPcr / 2.0) * 100))}%` }}
          />
        </div>
      </div>

      {/* 2. Max Pain Analysis */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-xl p-3.5 flex flex-col justify-between">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-400">
            <Target className="w-3.5 h-3.5 text-amber-400" />
            <span>Max Pain Theory</span>
          </div>
          <span className="text-[10px] text-slate-400 font-mono">Options Expiry Gravity</span>
        </div>

        <div className="flex items-baseline justify-between mb-1">
          <div>
            <div className="text-2xl font-black font-mono text-amber-400 tracking-tight">
              ₹{maxPain.maxPainStrike ? maxPain.maxPainStrike.toLocaleString('en-IN') : '—'}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">Max Pain Strike</div>
          </div>
          <div className="text-right font-mono">
            <div
              className={`text-sm font-bold ${
                maxPain.distanceFromSpot >= 0 ? 'text-emerald-400' : 'text-rose-400'
              }`}
            >
              {maxPain.distanceFromSpot > 0 ? '+' : ''}
              {maxPain.distanceFromSpot ? maxPain.distanceFromSpot.toFixed(1) : '0'} pts
            </div>
            <div className="text-[10px] text-slate-400">Spot Distance</div>
          </div>
        </div>

        <div className="text-[11px] text-slate-400 flex items-center justify-between pt-1 border-t border-slate-800/80">
          <span>Expiry Pinning Bias:</span>
          <span className="font-semibold text-slate-300 font-mono">
            {spotPrice > maxPain.maxPainStrike
              ? 'Downside Pull to Pain'
              : spotPrice < maxPain.maxPainStrike
              ? 'Upside Pull to Pain'
              : 'Pinned at Max Pain'}
          </span>
        </div>
      </div>

      {/* 3. Support & Resistance Walls */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-xl p-3.5 flex flex-col justify-between">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-400">
            <Shield className="w-3.5 h-3.5 text-indigo-400" />
            <span>Open Interest Walls</span>
          </div>
          <span className="text-[10px] text-slate-400 font-mono">Key Boundaries</span>
        </div>

        <div className="grid grid-cols-2 gap-2 mb-2 font-mono">
          <div className="bg-rose-950/20 border border-rose-500/20 rounded-lg p-2 text-center">
            <div className="text-[10px] text-rose-400 font-bold uppercase">Call Wall (Res)</div>
            <div className="text-base font-extrabold text-white mt-0.5">
              ₹{callWall ? callWall.toLocaleString('en-IN') : '—'}
            </div>
            <div className="text-[9px] text-slate-400">Highest Call OI</div>
          </div>
          <div className="bg-emerald-950/20 border border-emerald-500/20 rounded-lg p-2 text-center">
            <div className="text-[10px] text-emerald-400 font-bold uppercase">Put Wall (Sup)</div>
            <div className="text-base font-extrabold text-white mt-0.5">
              ₹{putWall ? putWall.toLocaleString('en-IN') : '—'}
            </div>
            <div className="text-[9px] text-slate-400">Highest Put OI</div>
          </div>
        </div>

        <div className="text-[11px] text-slate-400 flex items-center justify-between">
          <span>Expected Range:</span>
          <span className="font-bold text-slate-200 font-mono">
            {putWall ? putWall.toLocaleString('en-IN') : '—'} - {callWall ? callWall.toLocaleString('en-IN') : '—'}
          </span>
        </div>
      </div>

      {/* 4. OI Buildup Classifier */}
      <div className="bg-slate-900/80 border border-slate-800 rounded-xl p-3.5 flex flex-col justify-between">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-400">
            <BarChart3 className="w-3.5 h-3.5 text-purple-400" />
            <span>Strike Buildup Distribution</span>
          </div>
          <span className="text-[10px] text-slate-400 font-mono">Tick Analysis</span>
        </div>

        <div className="grid grid-cols-2 gap-x-2 gap-y-1 text-[11px] font-mono mb-2">
          <div className="flex items-center justify-between bg-emerald-950/20 px-2 py-1 rounded border border-emerald-500/10">
            <span className="text-emerald-400 flex items-center gap-1">
              <ArrowUpRight className="w-3 h-3" /> Long Buildup:
            </span>
            <span className="font-bold text-white">{pcr.longBuildupCount || 0}</span>
          </div>
          <div className="flex items-center justify-between bg-rose-950/20 px-2 py-1 rounded border border-rose-500/10">
            <span className="text-rose-400 flex items-center gap-1">
              <ArrowDownRight className="w-3 h-3" /> Short Buildup:
            </span>
            <span className="font-bold text-white">{pcr.shortBuildupCount || 0}</span>
          </div>
          <div className="flex items-center justify-between bg-cyan-950/20 px-2 py-1 rounded border border-cyan-500/10">
            <span className="text-cyan-400 flex items-center gap-1">
              <ArrowUpRight className="w-3 h-3" /> Short Covering:
            </span>
            <span className="font-bold text-white">{pcr.shortCoveringCount || 0}</span>
          </div>
          <div className="flex items-center justify-between bg-amber-950/20 px-2 py-1 rounded border border-amber-500/10">
            <span className="text-amber-400 flex items-center gap-1">
              <ArrowDownRight className="w-3 h-3" /> Long Unwind:
            </span>
            <span className="font-bold text-white">{pcr.longUnwindingCount || 0}</span>
          </div>
        </div>

        {/* OI Ratio Bar */}
        <div className="pt-1">
          <div className="flex justify-between text-[10px] font-mono text-slate-400 mb-0.5">
            <span className="text-rose-400">Calls {callOiPercent.toFixed(0)}%</span>
            <span className="text-emerald-400">Puts {putOiPercent.toFixed(0)}%</span>
          </div>
          <div className="w-full h-1.5 bg-slate-950 rounded-full flex overflow-hidden">
            <div className="bg-rose-500 h-full" style={{ width: `${callOiPercent}%` }} />
            <div className="bg-emerald-500 h-full" style={{ width: `${putOiPercent}%` }} />
          </div>
        </div>
      </div>
    </div>
  );
};
