import { AlertTriangle, ArrowRight, CheckCircle2, ChevronRight, Gauge, Layers, ShieldCheck, Sparkles, TrendingDown, TrendingUp, Zap } from 'lucide-react';
import React from 'react';
import type { FnOStrategyScoreDto } from '../types/fnoTrading';

interface StrategyConvictionCardProps {
  score: FnOStrategyScoreDto;
  onExecuteRecommendation: () => void;
}

export const StrategyConvictionCard: React.FC<StrategyConvictionCardProps> = ({
  score,
  onExecuteRecommendation,
}) => {
  const total = score.totalScore || 0;
  const isStrongBullish = total >= 60;
  const isStrongBearish = total <= -60;
  const isModerateBullish = total >= 30 && total < 60;
  const isModerateBearish = total <= -30 && total > -60;

  // Visual Gauge needle percent (from 0 to 100%, where 50% is 0 score)
  const needlePercent = Math.min(100, Math.max(0, ((total + 100) / 200) * 100));

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 shadow-xl mb-4">
      <div className="flex flex-col lg:flex-row items-center justify-between gap-6">
        {/* Left: Score Meter */}
        <div className="w-full lg:w-1/3 flex flex-col items-center justify-center p-3 bg-slate-950/60 rounded-xl border border-slate-800/80">
          <div className="flex items-center space-x-2 text-xs font-bold text-slate-400 mb-1">
            <Gauge className="w-4 h-4 text-cyan-400" />
            <span className="uppercase tracking-wider">Multi-Factor Conviction Score</span>
          </div>

          <div className="relative my-2 flex flex-col items-center">
            <span
              className={`text-5xl font-black font-mono tracking-tight ${
                isStrongBullish
                  ? 'text-emerald-400 drop-shadow-[0_0_15px_rgba(52,211,153,0.3)]'
                  : isStrongBearish
                  ? 'text-rose-400 drop-shadow-[0_0_15px_rgba(251,113,133,0.3)]'
                  : 'text-amber-300'
              }`}
            >
              {total > 0 ? `+${total.toFixed(0)}` : total.toFixed(0)}
            </span>
            <span className="text-[11px] font-mono text-slate-400 mt-1">Scale (-100 to +100)</span>
          </div>

          {/* Color-Coded Bar with Needle */}
          <div className="w-full max-w-xs mt-2">
            <div className="relative h-2.5 bg-slate-800 rounded-full overflow-hidden flex">
              <div className="w-1/3 bg-gradient-to-r from-rose-600 to-rose-400" />
              <div className="w-1/3 bg-gradient-to-r from-rose-400 via-amber-400 to-emerald-400" />
              <div className="w-1/3 bg-gradient-to-r from-emerald-400 to-emerald-600" />
            </div>
            {/* Needle indicator */}
            <div className="relative w-full h-3">
              <div
                className="absolute top-0 w-2 h-2 -ml-1 bg-white rounded-full shadow-md shadow-white/50 transition-all duration-500"
                style={{ left: `${needlePercent}%` }}
              />
            </div>
            <div className="flex justify-between text-[9px] font-mono text-slate-400">
              <span className="text-rose-400 font-bold">-100 (BEARISH)</span>
              <span className="text-slate-400">0 (NEUTRAL)</span>
              <span className="text-emerald-400 font-bold">+100 (BULLISH)</span>
            </div>
          </div>
        </div>

        {/* Center: The 4 Quantitative Pillars */}
        <div className="w-full lg:w-1/2 grid grid-cols-2 gap-2.5">
          {/* Pillar 1: PCR Sentiment */}
          <div className="bg-slate-950 p-2.5 rounded-lg border border-slate-800 flex flex-col justify-between">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-400 font-medium">1. PCR Sentiment</span>
              <span
                className={`font-mono font-bold text-xs ${
                  score.pcrScore >= 12
                    ? 'text-emerald-400'
                    : score.pcrScore <= -12
                    ? 'text-rose-400'
                    : 'text-slate-300'
                }`}
              >
                {score.pcrScore > 0 ? `+${score.pcrScore.toFixed(0)}` : score.pcrScore.toFixed(0)} / 25
              </span>
            </div>
            <div className="text-[10px] text-slate-400 mt-1">
              Put writing floor vs Call writing ceiling
            </div>
          </div>

          {/* Pillar 2: OI & Max Pain */}
          <div className="bg-slate-950 p-2.5 rounded-lg border border-slate-800 flex flex-col justify-between">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-400 font-medium">2. OI & Max Pain</span>
              <span
                className={`font-mono font-bold text-xs ${
                  score.oiMaxPainScore >= 12
                    ? 'text-emerald-400'
                    : score.oiMaxPainScore <= -12
                    ? 'text-rose-400'
                    : 'text-slate-300'
                }`}
              >
                {score.oiMaxPainScore > 0
                  ? `+${score.oiMaxPainScore.toFixed(0)}`
                  : score.oiMaxPainScore.toFixed(0)}{' '}
                / 25
              </span>
            </div>
            <div className="text-[10px] text-slate-400 mt-1">
              Buildup dominance & strike pinning gravity
            </div>
          </div>

          {/* Pillar 3: Greeks & IV */}
          <div className="bg-slate-950 p-2.5 rounded-lg border border-slate-800 flex flex-col justify-between">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-400 font-medium">3. Greeks & IV Regime</span>
              <span
                className={`font-mono font-bold text-xs ${
                  score.greeksIvScore >= 10
                    ? 'text-emerald-400'
                    : score.greeksIvScore <= -10
                    ? 'text-rose-400'
                    : 'text-slate-300'
                }`}
              >
                {score.greeksIvScore > 0
                  ? `+${score.greeksIvScore.toFixed(0)}`
                  : score.greeksIvScore.toFixed(0)}{' '}
                / 25
              </span>
            </div>
            <div className="text-[10px] text-slate-400 mt-1">
              ATM Delta skew & volatility crush protection
            </div>
          </div>

          {/* Pillar 4: Spot Trend & EMA */}
          <div className="bg-slate-950 p-2.5 rounded-lg border border-slate-800 flex flex-col justify-between">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-400 font-medium">4. Spot Momentum</span>
              <span
                className={`font-mono font-bold text-xs ${
                  score.spotTrendScore >= 12
                    ? 'text-emerald-400'
                    : score.spotTrendScore <= -12
                    ? 'text-rose-400'
                    : 'text-slate-300'
                }`}
              >
                {score.spotTrendScore > 0
                  ? `+${score.spotTrendScore.toFixed(0)}`
                  : score.spotTrendScore.toFixed(0)}{' '}
                / 25
              </span>
            </div>
            <div className="text-[10px] text-slate-400 mt-1">
              9/21/50 EMA Triad & Supertrend alignment
            </div>
          </div>
        </div>

        {/* Right: Recommendation & Execution Action */}
        <div className="w-full lg:w-1/3 bg-slate-950/80 border border-slate-800 rounded-xl p-3.5 flex flex-col justify-between">
          <div className="flex items-center justify-between mb-2">
            <div className="text-[11px] font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
              <Sparkles className="w-3.5 h-3.5 text-cyan-400" />
              <span>Engine Recommendation</span>
            </div>
            <span
              className={`text-[10px] font-bold px-2 py-0.5 rounded font-mono ${
                isStrongBullish
                  ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                  : isStrongBearish
                  ? 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                  : 'bg-slate-800 text-slate-300'
              }`}
            >
              {score.recommendation}
            </span>
          </div>

          <div className="bg-slate-900 p-2.5 rounded-lg border border-slate-800 mb-2">
            <div className="text-sm font-extrabold text-white font-mono flex items-center justify-between">
              <span>{score.recommendedStrike || 'No Action'}</span>
              {score.recommendedEntryPrice > 0 && (
                <span className="text-cyan-400 font-mono text-xs">
                  Est. ₹{score.recommendedEntryPrice.toFixed(1)}
                </span>
              )}
            </div>
            <p className="text-[11px] text-slate-300 mt-1 leading-relaxed">
              {score.setupRationale}
            </p>
          </div>

          {/* Action Button */}
          {isStrongBullish || isStrongBearish ? (
            <button
              onClick={onExecuteRecommendation}
              className={`w-full py-2 px-3 rounded-lg text-xs font-bold font-mono transition flex items-center justify-center space-x-1.5 shadow-lg ${
                isStrongBullish
                  ? 'bg-emerald-600 hover:bg-emerald-500 text-white shadow-emerald-600/30'
                  : 'bg-rose-600 hover:bg-rose-500 text-white shadow-rose-600/30'
              }`}
            >
              <Zap className="w-3.5 h-3.5 fill-current" />
              <span>1-CLICK EXECUTE {score.recommendedStrike}</span>
            </button>
          ) : (
            <div className="text-center py-1.5 bg-slate-900/50 rounded text-xs text-slate-400 font-mono">
              Conviction below ±60 threshold — Observation Mode
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
