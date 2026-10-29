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
    <div
      className={`ob-panel rounded-2xl p-4 shadow-2xl mb-4 relative overflow-hidden transition-all duration-700 border border-[#1A212D] ${
        isStrongBullish ? 'ob-glow-bull border-[#19E58C]/30' : isStrongBearish ? 'ob-glow-bear border-[#FF4D67]/30' : 'ob-glow-brand border-[#7B7BFF]/30'
      }`}
    >
      {/* Scanner Radar Beam across Conviction Card */}
      <div className="ob-scanner-beam" />

      <div className="flex flex-col lg:flex-row items-center justify-between gap-6 relative z-10">
        {/* Left: Score Meter */}
        <div className="w-full lg:w-1/3 flex flex-col items-center justify-center p-4 bg-[#06080C]/90 rounded-xl border border-[#1A212D] shadow-inner ob-hover-lift">
          <div className="flex items-center space-x-2 text-xs font-bold text-slate-400 mb-1">
            <Gauge className="w-4 h-4 text-[#45B8FF] animate-pulse" />
            <span className="uppercase tracking-wider font-mono">QUANTUM FACTOR CONVICTION</span>
          </div>

          <div className="relative my-2.5 flex flex-col items-center">
            <span
              className={`text-5xl font-black font-mono tracking-tight ${
                isStrongBullish
                  ? 'ob-grad-bull drop-shadow-[0_0_20px_rgba(25,229,140,0.4)]'
                  : isStrongBearish
                  ? 'ob-grad-bear drop-shadow-[0_0_20px_rgba(255,77,103,0.4)]'
                  : 'ob-grad-brand drop-shadow-[0_0_20px_rgba(123,123,255,0.4)]'
              }`}
            >
              {total > 0 ? `+${total.toFixed(0)}` : total.toFixed(0)}
            </span>
            <span className="text-[11px] font-mono text-slate-400 mt-1 flex items-center gap-1.5">
              <span className={`w-1.5 h-1.5 rounded-full ${isStrongBullish ? 'bg-[#19E58C]' : isStrongBearish ? 'bg-[#FF4D67]' : 'bg-[#7B7BFF]'} animate-ping`} />
              Scale (-100 to +100)
            </span>
          </div>

          {/* Color-Coded Bar with Needle */}
          <div className="w-full max-w-xs mt-2">
            <div className="relative h-2.5 bg-[#11161F] rounded-full overflow-hidden flex border border-[#1A212D]">
              <div className="w-1/3 bg-gradient-to-r from-[#FF4D67] to-[#F472B6]" />
              <div className="w-1/3 bg-gradient-to-r from-[#F472B6] via-[#FFB020] to-[#2DD4BF]" />
              <div className="w-1/3 bg-gradient-to-r from-[#2DD4BF] to-[#19E58C]" />
            </div>
            {/* Needle indicator */}
            <div className="relative w-full h-3">
              <div
                className="absolute top-0 w-2.5 h-2.5 -ml-1 bg-white rounded-full shadow-[0_0_8px_#ffffff] transition-all duration-700 ease-out"
                style={{ left: `${needlePercent}%` }}
              />
            </div>
            <div className="flex justify-between text-[9px] font-mono text-slate-400">
              <span className="text-[#FF4D67] font-bold">-100 (BEARISH)</span>
              <span className="text-slate-400">0 (NEUTRAL)</span>
              <span className="text-[#19E58C] font-bold">+100 (BULLISH)</span>
            </div>
          </div>
        </div>

        {/* Center: The 4 Quantitative Pillars */}
        <div className="w-full lg:w-1/2 grid grid-cols-2 gap-3">
          {/* Pillar 1: PCR Sentiment */}
          <div className="bg-[#06080C]/80 p-3 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-300 font-semibold font-mono">1. PCR Sentiment</span>
              <span
                className={`font-mono font-bold text-xs ${
                  score.pcrScore >= 12
                    ? 'text-[#19E58C]'
                    : score.pcrScore <= -12
                    ? 'text-[#FF4D67]'
                    : 'text-slate-300'
                }`}
              >
                {score.pcrScore > 0 ? `+${score.pcrScore.toFixed(0)}` : score.pcrScore.toFixed(0)} / 25
              </span>
            </div>
            <div className="w-full bg-[#11161F] h-1 rounded-full my-2 overflow-hidden">
              <div
                className={`h-full ${score.pcrScore >= 0 ? 'bg-[#19E58C]' : 'bg-[#FF4D67]'}`}
                style={{ width: `${Math.min(100, (Math.abs(score.pcrScore) / 25) * 100)}%` }}
              />
            </div>
            <div className="text-[10px] text-slate-400">
              Put writing floor vs Call writing ceiling
            </div>
          </div>

          {/* Pillar 2: OI & Max Pain */}
          <div className="bg-[#06080C]/80 p-3 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-300 font-semibold font-mono">2. OI & Max Pain</span>
              <span
                className={`font-mono font-bold text-xs ${
                  score.oiMaxPainScore >= 12
                    ? 'text-[#19E58C]'
                    : score.oiMaxPainScore <= -12
                    ? 'text-[#FF4D67]'
                    : 'text-slate-300'
                }`}
              >
                {score.oiMaxPainScore > 0
                  ? `+${score.oiMaxPainScore.toFixed(0)}`
                  : score.oiMaxPainScore.toFixed(0)}{' '}
                / 25
              </span>
            </div>
            <div className="w-full bg-[#11161F] h-1 rounded-full my-2 overflow-hidden">
              <div
                className={`h-full ${score.oiMaxPainScore >= 0 ? 'bg-[#19E58C]' : 'bg-[#FF4D67]'}`}
                style={{ width: `${Math.min(100, (Math.abs(score.oiMaxPainScore) / 25) * 100)}%` }}
              />
            </div>
            <div className="text-[10px] text-slate-400">
              Buildup dominance & strike pinning gravity
            </div>
          </div>

          {/* Pillar 3: Greeks & IV */}
          <div className="bg-[#06080C]/80 p-3 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-300 font-semibold font-mono">3. Greeks & IV Regime</span>
              <span
                className={`font-mono font-bold text-xs ${
                  score.greeksIvScore >= 10
                    ? 'text-[#19E58C]'
                    : score.greeksIvScore <= -10
                    ? 'text-[#FF4D67]'
                    : 'text-slate-300'
                }`}
              >
                {score.greeksIvScore > 0
                  ? `+${score.greeksIvScore.toFixed(0)}`
                  : score.greeksIvScore.toFixed(0)}{' '}
                / 25
              </span>
            </div>
            <div className="w-full bg-[#11161F] h-1 rounded-full my-2 overflow-hidden">
              <div
                className={`h-full ${score.greeksIvScore >= 0 ? 'bg-[#19E58C]' : 'bg-[#FF4D67]'}`}
                style={{ width: `${Math.min(100, (Math.abs(score.greeksIvScore) / 25) * 100)}%` }}
              />
            </div>
            <div className="text-[10px] text-slate-400">
              ATM Delta skew & volatility crush protection
            </div>
          </div>

          {/* Pillar 4: Spot Trend & EMA */}
          <div className="bg-[#06080C]/80 p-3 rounded-xl border border-[#1A212D] flex flex-col justify-between ob-hover-lift">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-300 font-semibold font-mono">4. Spot Momentum</span>
              <span
                className={`font-mono font-bold text-xs ${
                  score.spotTrendScore >= 12
                    ? 'text-[#19E58C]'
                    : score.spotTrendScore <= -12
                    ? 'text-[#FF4D67]'
                    : 'text-slate-300'
                }`}
              >
                {score.spotTrendScore > 0
                  ? `+${score.spotTrendScore.toFixed(0)}`
                  : score.spotTrendScore.toFixed(0)}{' '}
                / 25
              </span>
            </div>
            <div className="w-full bg-[#11161F] h-1 rounded-full my-2 overflow-hidden">
              <div
                className={`h-full ${score.spotTrendScore >= 0 ? 'bg-[#19E58C]' : 'bg-[#FF4D67]'}`}
                style={{ width: `${Math.min(100, (Math.abs(score.spotTrendScore) / 25) * 100)}%` }}
              />
            </div>
            <div className="text-[10px] text-slate-400">
              9/21/50 EMA Triad & Supertrend alignment
            </div>
          </div>
        </div>

        {/* Right: Recommendation & Execution Action */}
        <div className="w-full lg:w-1/3 bg-[#06080C]/90 border border-[#1A212D] rounded-xl p-4 flex flex-col justify-between ob-hover-lift">
          <div className="flex items-center justify-between mb-2.5">
            <div className="text-[11px] font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5 font-mono">
              <Sparkles className="w-3.5 h-3.5 text-[#45B8FF] animate-pulse" />
              <span>QUANTUM STRATEGY RADAR</span>
            </div>
            <span
              className={`text-[10px] font-bold px-2 py-0.5 rounded font-mono ${
                isStrongBullish
                  ? 'bg-[#19E58C]/10 text-[#19E58C] border border-[#19E58C]/40 shadow-[0_0_10px_rgba(25,229,140,0.25)]'
                  : isStrongBearish
                  ? 'bg-[#FF4D67]/10 text-[#FF4D67] border border-[#FF4D67]/40 shadow-[0_0_10px_rgba(255,77,103,0.25)]'
                  : 'bg-[#1A212D] text-slate-300'
              }`}
            >
              {score.recommendation}
            </span>
          </div>

          <div className={`p-3 rounded-lg mb-3 ${isStrongBullish ? 'ob-zone-box' : isStrongBearish ? 'ob-zone-box-bear' : 'bg-[#11161F] border border-[#1A212D]'}`}>
            <div className="text-sm font-extrabold text-white font-mono flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className={`w-2 h-2 rounded-full ${isStrongBullish ? 'bg-[#19E58C]' : isStrongBearish ? 'bg-[#FF4D67]' : 'bg-slate-400'} animate-ping`} />
                {score.recommendedStrike || 'No Action'}
              </span>
              {score.recommendedEntryPrice > 0 && (
                <span className="text-[#45B8FF] font-mono text-xs font-bold">
                  Est. ₹{score.recommendedEntryPrice.toFixed(1)}
                </span>
              )}
            </div>
            <p className="text-[11px] text-slate-300 mt-2 leading-relaxed font-sans">
              {score.setupRationale}
            </p>
          </div>

          {/* Action Button */}
          {isStrongBullish || isStrongBearish ? (
            <button
              onClick={onExecuteRecommendation}
              className={`w-full py-2.5 px-3 rounded-lg text-xs font-bold font-mono transition flex items-center justify-center space-x-2 shadow-xl ${
                isStrongBullish ? 'ob-btn-bull text-white' : 'ob-btn-bear text-white'
              }`}
            >
              <Zap className="w-4 h-4 fill-current animate-bounce" />
              <span>1-CLICK EXECUTE {score.recommendedStrike}</span>
            </button>
          ) : (
            <div className="text-center py-2 bg-[#11161F] rounded-lg text-xs text-slate-400 font-mono border border-[#1A212D]">
              Conviction below ±60 threshold — Observation Mode
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
