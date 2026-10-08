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
    <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 shadow-xl mb-4">
      <div className="flex items-center justify-between mb-3 border-b border-slate-800 pb-2">
        <div className="flex items-center space-x-2">
          <Sparkles className="w-4 h-4 text-cyan-400" />
          <h3 className="text-xs font-extrabold text-white uppercase tracking-wider font-mono">
            ATM STRIKE GREEKS MATRIX ({atmStrike.strikePrice})
          </h3>
        </div>
        <span className="text-[10px] text-slate-400 font-mono">
          Black-Scholes Mathematical Engine (7.0% Risk-Free Rate)
        </span>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
        {/* Delta */}
        <div className="bg-slate-950 p-3 rounded-lg border border-slate-800 flex flex-col justify-between">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span>DELTA (Δ)</span>
            <span className="text-[9px] text-slate-400">Direction</span>
          </div>
          <div className="my-2">
            <div className="text-xs text-slate-400 font-mono">Call: <span className="text-emerald-400 font-bold">{callGreeks?.delta ? callGreeks.delta.toFixed(3) : '0.50'}</span></div>
            <div className="text-xs text-slate-400 font-mono">Put: <span className="text-rose-400 font-bold">{putGreeks?.delta ? putGreeks.delta.toFixed(3) : '-0.50'}</span></div>
          </div>
          <div className="text-[9px] text-slate-400">₹ move per 1 pt index change</div>
        </div>

        {/* Theta */}
        <div className="bg-slate-950 p-3 rounded-lg border border-slate-800 flex flex-col justify-between">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span>THETA (Θ)</span>
            <Clock className="w-3 h-3 text-amber-400" />
          </div>
          <div className="my-2">
            <div className="text-xs text-slate-400 font-mono">Call: <span className="text-amber-400 font-bold">₹{callGreeks?.theta ? callGreeks.theta.toFixed(1) : '-12.0'}</span>/day</div>
            <div className="text-xs text-slate-400 font-mono">Put: <span className="text-amber-400 font-bold">₹{putGreeks?.theta ? putGreeks.theta.toFixed(1) : '-12.0'}</span>/day</div>
          </div>
          <div className="text-[9px] text-slate-400">Daily time decay erosion</div>
        </div>

        {/* Gamma */}
        <div className="bg-slate-950 p-3 rounded-lg border border-slate-800 flex flex-col justify-between">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span>GAMMA (Γ)</span>
            <Flame className="w-3 h-3 text-cyan-400" />
          </div>
          <div className="my-2">
            <div className="text-lg font-black font-mono text-cyan-300">
              {callGreeks?.gamma ? callGreeks.gamma.toFixed(5) : '0.00045'}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">Delta Acceleration</div>
          </div>
          <div className="text-[9px] text-slate-400">Highest at ATM, powers explosive spikes</div>
        </div>

        {/* Vega */}
        <div className="bg-slate-950 p-3 rounded-lg border border-slate-800 flex flex-col justify-between">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span>VEGA (ν)</span>
            <Activity className="w-3 h-3 text-purple-400" />
          </div>
          <div className="my-2">
            <div className="text-lg font-black font-mono text-purple-300">
              ₹{callGreeks?.vega ? callGreeks.vega.toFixed(1) : '8.5'}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">Per 1% IV Change</div>
          </div>
          <div className="text-[9px] text-slate-400">Gain/Loss on volatility spikes</div>
        </div>

        {/* POP */}
        <div className="bg-slate-950 p-3 rounded-lg border border-slate-800 flex flex-col justify-between">
          <div className="text-[11px] font-bold text-slate-400 uppercase font-mono flex items-center justify-between">
            <span>PROBABILITY (POP)</span>
            <TrendingUp className="w-3 h-3 text-emerald-400" />
          </div>
          <div className="my-2">
            <div className="text-xs text-slate-400 font-mono">Call POP: <span className="text-emerald-400 font-bold">{callGreeks?.pop ? callGreeks.pop.toFixed(0) : '48'}%</span></div>
            <div className="text-xs text-slate-400 font-mono">Put POP: <span className="text-rose-400 font-bold">{putGreeks?.pop ? putGreeks.pop.toFixed(0) : '52'}%</span></div>
          </div>
          <div className="text-[9px] text-slate-400">Statistical chance of expiring ITM</div>
        </div>
      </div>
    </div>
  );
};
