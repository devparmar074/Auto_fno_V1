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
      <div className="ob-panel ob-hover-lift relative overflow-hidden rounded-xl p-3.5 flex flex-col justify-between border border-[#1A212D]">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-400">
            <Compass className="w-3.5 h-3.5 text-[#45B8FF] animate-spin-slow" />
            <span className="font-mono tracking-wider">PUT-CALL RATIO (PCR)</span>
          </div>
          <span
            className={`text-[10px] font-bold px-2 py-0.5 rounded uppercase font-mono transition-all ${
              isPcrBullish
                ? 'bg-[#19E58C]/10 text-[#19E58C] border border-[#19E58C]/40 shadow-[0_0_10px_rgba(25,229,140,0.25)]'
                : isPcrBearish
                ? 'bg-[#FF4D67]/10 text-[#FF4D67] border border-[#FF4D67]/40 shadow-[0_0_10px_rgba(255,77,103,0.25)]'
                : 'bg-[#1A212D] text-slate-300 border border-slate-700/50'
            }`}
          >
            {pcr.sentiment || 'Neutral'}
          </span>
        </div>

        <div className="flex items-baseline justify-between mb-2.5">
          <div>
            <div className={`text-2xl font-black font-mono tracking-tight ${
              isPcrBullish ? 'text-[#19E58C] drop-shadow-[0_0_10px_rgba(25,229,140,0.3)]' :
              isPcrBearish ? 'text-[#FF4D67] drop-shadow-[0_0_10px_rgba(255,77,103,0.3)]' :
              'text-white'
            }`}>
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

        {/* Visual Metallic Shimmer Slider */}
        <div className="space-y-1.5">
          <div className="w-full bg-[#06080C] rounded-full h-2 overflow-hidden flex border border-[#1A212D] relative">
            <div
              className={`h-full transition-all duration-700 ${
                isPcrBullish ? 'ob-glint-bar' : isPcrBearish ? 'ob-glint-bar-bear' : 'bg-gradient-to-r from-[#FF4D67] via-[#FFB020] to-[#19E58C]'
              }`}
              style={{ width: `${Math.min(100, Math.max(0, (oiPcr / 2.0) * 100))}%` }}
            />
          </div>
          {/* Liquidity Stream with Travelling Spark */}
          <div className="ob-liq-line">
            <div className="ob-liq-spark" />
          </div>
        </div>
      </div>

      {/* 2. Max Pain Analysis */}
      <div className="ob-panel ob-hover-lift relative overflow-hidden rounded-xl p-3.5 flex flex-col justify-between border border-[#1A212D]">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-400">
            <Target className="w-3.5 h-3.5 text-[#FFB020]" />
            <span className="font-mono tracking-wider">MAX PAIN THEORY</span>
          </div>
          <span className="text-[10px] text-slate-400 font-mono">Expiry Pinning Gravity</span>
        </div>

        <div className="flex items-baseline justify-between mb-2">
          <div>
            <div className="text-2xl font-black font-mono tracking-tight text-[#FFB020] drop-shadow-[0_0_12px_rgba(255,176,32,0.35)]">
              ₹{maxPain.maxPainStrike ? maxPain.maxPainStrike.toLocaleString('en-IN') : '—'}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">Max Pain Strike</div>
          </div>
          <div className="text-right font-mono">
            <div
              className={`text-sm font-bold ${
                maxPain.distanceFromSpot >= 0 ? 'text-[#19E58C]' : 'text-[#FF4D67]'
              }`}
            >
              {maxPain.distanceFromSpot > 0 ? '+' : ''}
              {maxPain.distanceFromSpot ? maxPain.distanceFromSpot.toFixed(1) : '0'} pts
            </div>
            <div className="text-[10px] text-slate-400">Spot Distance</div>
          </div>
        </div>

        <div className="text-[11px] text-slate-400 flex items-center justify-between pt-2 border-t border-[#1A212D]">
          <span className="flex items-center gap-1.5">
            <span className="w-1.5 h-1.5 rounded-full bg-[#FFB020] animate-ping" />
            <span>Pinning Bias:</span>
          </span>
          <span className="font-semibold text-slate-200 font-mono">
            {spotPrice > maxPain.maxPainStrike
              ? 'Downside Pull to Pain'
              : spotPrice < maxPain.maxPainStrike
              ? 'Upside Pull to Pain'
              : 'Pinned at Max Pain'}
          </span>
        </div>
      </div>

      {/* 3. Support & Resistance Walls */}
      <div className="ob-panel ob-hover-lift relative overflow-hidden rounded-xl p-3.5 flex flex-col justify-between border border-[#1A212D]">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-400">
            <Shield className="w-3.5 h-3.5 text-[#7B7BFF]" />
            <span className="font-mono tracking-wider">OPEN INTEREST WALLS</span>
          </div>
          <span className="text-[10px] text-slate-400 font-mono">Quantum Barriers</span>
        </div>

        <div className="grid grid-cols-2 gap-2 mb-2 font-mono">
          <div className="ob-zone-box-bear p-2 text-center rounded-lg">
            <div className="text-[10px] text-[#FF4D67] font-bold uppercase tracking-wider">Call Wall (Res)</div>
            <div className="text-base font-extrabold text-white mt-0.5 drop-shadow-sm">
              ₹{callWall ? callWall.toLocaleString('en-IN') : '—'}
            </div>
            <div className="text-[9px] text-slate-400">Highest Call OI</div>
          </div>
          <div className="ob-zone-box p-2 text-center rounded-lg">
            <div className="text-[10px] text-[#19E58C] font-bold uppercase tracking-wider">Put Wall (Sup)</div>
            <div className="text-base font-extrabold text-white mt-0.5 drop-shadow-sm">
              ₹{putWall ? putWall.toLocaleString('en-IN') : '—'}
            </div>
            <div className="text-[9px] text-slate-400">Highest Put OI</div>
          </div>
        </div>

        <div className="text-[11px] text-slate-400 flex items-center justify-between pt-1 border-t border-[#1A212D]">
          <span>Expected Range:</span>
          <span className="font-bold text-slate-200 font-mono">
            {putWall ? putWall.toLocaleString('en-IN') : '—'} - {callWall ? callWall.toLocaleString('en-IN') : '—'}
          </span>
        </div>
      </div>

      {/* 4. OI Buildup Classifier */}
      <div className="ob-panel ob-hover-lift relative overflow-hidden rounded-xl p-3.5 flex flex-col justify-between border border-[#1A212D]">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center space-x-1.5 text-xs font-semibold text-slate-400">
            <BarChart3 className="w-3.5 h-3.5 text-[#A78BFA]" />
            <span className="font-mono tracking-wider">BUILDUP DISTRIBUTION</span>
          </div>
          <span className="text-[10px] text-slate-400 font-mono">Live Pulse</span>
        </div>

        <div className="grid grid-cols-2 gap-x-2 gap-y-1 text-[11px] font-mono mb-2">
          <div className="flex items-center justify-between ob-buildup-long px-2 py-1 rounded">
            <span className="text-[#19E58C] flex items-center gap-1 font-semibold">
              <ArrowUpRight className="w-3 h-3" /> Long Buildup:
            </span>
            <span className="font-bold text-white">{pcr.longBuildupCount || 0}</span>
          </div>
          <div className="flex items-center justify-between ob-buildup-short px-2 py-1 rounded">
            <span className="text-[#FF4D67] flex items-center gap-1 font-semibold">
              <ArrowDownRight className="w-3 h-3" /> Short Buildup:
            </span>
            <span className="font-bold text-white">{pcr.shortBuildupCount || 0}</span>
          </div>
          <div className="flex items-center justify-between ob-buildup-cover px-2 py-1 rounded">
            <span className="text-[#45B8FF] flex items-center gap-1 font-semibold">
              <ArrowUpRight className="w-3 h-3" /> Short Covering:
            </span>
            <span className="font-bold text-white">{pcr.shortCoveringCount || 0}</span>
          </div>
          <div className="flex items-center justify-between ob-buildup-unwind px-2 py-1 rounded">
            <span className="text-[#FFB020] flex items-center gap-1 font-semibold">
              <ArrowDownRight className="w-3 h-3" /> Long Unwind:
            </span>
            <span className="font-bold text-white">{pcr.longUnwindingCount || 0}</span>
          </div>
        </div>

        {/* OI Ratio Bar */}
        <div className="pt-1 border-t border-[#1A212D]">
          <div className="flex justify-between text-[10px] font-mono text-slate-400 mb-1">
            <span className="text-[#FF4D67] font-bold">Calls {callOiPercent.toFixed(0)}%</span>
            <span className="text-[#19E58C] font-bold">Puts {putOiPercent.toFixed(0)}%</span>
          </div>
          <div className="w-full h-1.5 bg-[#06080C] rounded-full flex overflow-hidden border border-[#1A212D]">
            <div className="bg-[#FF4D67] h-full" style={{ width: `${callOiPercent}%` }} />
            <div className="bg-[#19E58C] h-full" style={{ width: `${putOiPercent}%` }} />
          </div>
        </div>
      </div>
    </div>
  );
};
