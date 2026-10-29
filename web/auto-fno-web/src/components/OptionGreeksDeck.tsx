import { Activity, Clock, Flame, ShieldAlert, Sparkles, TrendingUp } from 'lucide-react';
import React from 'react';
import type { OptionChainDto, StrikeDataDto } from '../types/fnoTrading';

interface OptionGreeksDeckProps {
  chain: OptionChainDto;
}

export const OptionGreeksDeck: React.FC<OptionGreeksDeckProps> = ({ chain }) => {
  const spot = chain.spotPrice || 0;
  const strikes = chain.strikes || [];

  const atmStrike = strikes.find((s) => s.isAtm) || strikes.find((s) => s.strikePrice === chain.atmStrike);

  if (!atmStrike) return null;

  const callGreeks = atmStrike.call?.greeks;
  const putGreeks = atmStrike.put?.greeks;

  return (
    <div className="ob-panel rounded-2xl p-4 shadow-2xl mb-4 border border-[#1A212D] relative overflow-hidden">
      {/* Scanner Radar Beam */}
      <div className="ob-scanner-beam" />

      <div className="flex items-center justify-between mb-3 border-b border-[#1A212D] pb-2.5 relative z-10">
        <div className="flex items-center space-x-2">
          <Sparkles className="w-4 h-4 text-[#45B8FF] animate-pulse" />
          <h3 className="text-xs font-extrabold text-white uppercase tracking-wider font-mono">
            <span className="ob-grad-brand">ATM STRIKE GREEKS MATRIX</span> ({atmStrike.strikePrice})
          </h3>
        </div>
        <span className="text-[10px] text-slate-400 font-mono flex items-center gap-1.5">
          <span className="w-1.5 h-1.5 rounded-full bg-[#2DD4BF] animate-ping" />
          Black-Scholes Mathematical Engine (7.0% Risk-Free Rate)
        </span>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-5 gap-3 relative z-10">
        {/* Delta */}
        <div className="bg-[#06080C]/90 p-3.5 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift shadow-inner">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span className="tracking-wider">DELTA (Δ)</span>
            <span className="text-[9px] text-[#45B8FF] font-bold">Direction</span>
          </div>
          <div className="my-2.5 space-y-1">
            <div className="text-xs text-slate-300 font-mono flex justify-between">
              <span>Call:</span>
              <span className="text-[#19E58C] font-bold font-mono text-sm">{callGreeks?.delta ? callGreeks.delta.toFixed(3) : '0.500'}</span>
            </div>
            <div className="text-xs text-slate-300 font-mono flex justify-between">
              <span>Put:</span>
              <span className="text-[#FF4D67] font-bold font-mono text-sm">{putGreeks?.delta ? putGreeks.delta.toFixed(3) : '-0.500'}</span>
            </div>
          </div>
          <div className="w-full bg-[#11161F] h-1 rounded-full overflow-hidden mb-1.5">
            <div className="ob-glint-bar h-full" style={{ width: `${Math.abs(callGreeks?.delta || 0.5) * 100}%` }} />
          </div>
          <div className="text-[9px] text-slate-400">₹ move per 1 pt index change</div>
        </div>

        {/* Theta */}
        <div className="bg-[#06080C]/90 p-3.5 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift shadow-inner">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span className="tracking-wider">THETA (Θ)</span>
            <Clock className="w-3.5 h-3.5 text-[#FFB020] animate-spin-slow" />
          </div>
          <div className="my-2.5 space-y-1">
            <div className="text-xs text-slate-300 font-mono flex justify-between">
              <span>Call:</span>
              <span className="text-[#FFB020] font-bold font-mono text-sm">₹{callGreeks?.theta ? callGreeks.theta.toFixed(1) : '-12.0'}<span className="text-[10px] text-slate-400">/d</span></span>
            </div>
            <div className="text-xs text-slate-300 font-mono flex justify-between">
              <span>Put:</span>
              <span className="text-[#FFB020] font-bold font-mono text-sm">₹{putGreeks?.theta ? putGreeks.theta.toFixed(1) : '-12.0'}<span className="text-[10px] text-slate-400">/d</span></span>
            </div>
          </div>
          <div className="w-full bg-[#11161F] h-1 rounded-full overflow-hidden mb-1.5">
            <div className="bg-[#FFB020] h-full" style={{ width: '65%' }} />
          </div>
          <div className="text-[9px] text-slate-400">Daily time decay erosion</div>
        </div>

        {/* Gamma */}
        <div className="bg-[#06080C]/90 p-3.5 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift shadow-inner">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span className="tracking-wider">GAMMA (Γ)</span>
            <Flame className="w-3.5 h-3.5 text-[#45B8FF] animate-pulse" />
          </div>
          <div className="my-2">
            <div className="text-lg font-black font-mono text-[#45B8FF] drop-shadow-[0_0_10px_rgba(69,184,255,0.3)]">
              {callGreeks?.gamma ? callGreeks.gamma.toFixed(5) : '0.00045'}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">Delta Acceleration</div>
          </div>
          <div className="w-full bg-[#11161F] h-1 rounded-full overflow-hidden mb-1.5">
            <div className="ob-glint-bar h-full" style={{ width: '80%' }} />
          </div>
          <div className="text-[9px] text-slate-400">Highest at ATM, powers spikes</div>
        </div>

        {/* Vega */}
        <div className="bg-[#06080C]/90 p-3.5 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift shadow-inner">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span className="tracking-wider">VEGA (ν)</span>
            <Activity className="w-3.5 h-3.5 text-[#A78BFA] animate-bounce" />
          </div>
          <div className="my-2">
            <div className="text-lg font-black font-mono text-[#A78BFA] drop-shadow-[0_0_10px_rgba(167,139,250,0.3)]">
              ₹{callGreeks?.vega ? callGreeks.vega.toFixed(1) : '8.5'}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">Per 1% IV Change</div>
          </div>
          <div className="w-full bg-[#11161F] h-1 rounded-full overflow-hidden mb-1.5">
            <div className="bg-[#A78BFA] h-full" style={{ width: '55%' }} />
          </div>
          <div className="text-[9px] text-slate-400">Gain/Loss on volatility spikes</div>
        </div>

        {/* POP */}
        <div className="bg-[#06080C]/90 p-3.5 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift shadow-inner">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span className="tracking-wider">PROBABILITY (POP)</span>
            <TrendingUp className="w-3.5 h-3.5 text-[#19E58C]" />
          </div>
          <div className="my-2.5 space-y-1">
            <div className="text-xs text-slate-300 font-mono flex justify-between">
              <span>Call POP:</span>
              <span className="text-[#19E58C] font-bold font-mono text-sm">{callGreeks?.pop ? callGreeks.pop.toFixed(0) : '48'}%</span>
            </div>
            <div className="text-xs text-slate-300 font-mono flex justify-between">
              <span>Put POP:</span>
              <span className="text-[#FF4D67] font-bold font-mono text-sm">{putGreeks?.pop ? putGreeks.pop.toFixed(0) : '52'}%</span>
            </div>
          </div>
          <div className="w-full h-1 bg-[#11161F] rounded-full flex overflow-hidden mb-1.5">
            <div className="bg-[#19E58C] h-full" style={{ width: `${callGreeks?.pop || 48}%` }} />
            <div className="bg-[#FF4D67] h-full" style={{ width: `${putGreeks?.pop || 52}%` }} />
          </div>
          <div className="text-[9px] text-slate-400">Statistical chance of ITM expiry</div>
        </div>
      </div>
    </div>
  );
};
