import { AlertCircle, CheckCircle2, ShieldCheck, X, Zap } from 'lucide-react';
import React, { useState } from 'react';
import type { FnOStrategyScoreDto } from '../types/fnoTrading';

interface SemiAutoAlertModalProps {
  alert: FnOStrategyScoreDto;
  onConfirm: (lots: number) => void;
  onDismiss: () => void;
  isExecuting: boolean;
}

export const SemiAutoAlertModal: React.FC<SemiAutoAlertModalProps> = ({
  alert,
  onConfirm,
  onDismiss,
  isExecuting,
}) => {
  const [lots, setLots] = useState(1);
  const [confirmedSafety, setConfirmedSafety] = useState(false);

  const isBuyCall = alert.recommendation === 'StrongBuyCall';
  const actionColor = isBuyCall ? 'text-emerald-400' : 'text-rose-400';
  const actionBg = isBuyCall ? 'bg-emerald-600 hover:bg-emerald-500' : 'bg-rose-600 hover:bg-rose-500';

  return (
    <div className="fixed inset-0 bg-black/85 backdrop-blur-md z-50 flex items-center justify-center p-4">
      <div
        className={`ob-panel rounded-2xl max-w-lg w-full p-6 shadow-2xl relative border overflow-hidden transition-all duration-300 ${
          isBuyCall ? 'border-[#19E58C]/40 ob-glow-bull' : 'border-[#FF4D67]/40 ob-glow-bear'
        }`}
      >
        {/* Scanner Radar Beam across Modal */}
        <div className="ob-scanner-beam" />

        <button
          onClick={onDismiss}
          className="absolute top-4 right-4 text-slate-400 hover:text-white transition ob-hover-lift z-20"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Top Header */}
        <div className="flex items-center space-x-3 mb-4 relative z-10">
          <div
            className={`w-10 h-10 rounded-xl flex items-center justify-center ${
              isBuyCall ? 'bg-[#19E58C]/20 text-[#19E58C] border border-[#19E58C]/40 shadow-[0_0_12px_rgba(25,229,140,0.3)]' : 'bg-[#FF4D67]/20 text-[#FF4D67] border border-[#FF4D67]/40 shadow-[0_0_12px_rgba(255,77,103,0.3)]'
            }`}
          >
            <Zap className="w-6 h-6 fill-current animate-bounce" />
          </div>
          <div>
            <div className="text-[10px] font-mono uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
              <span className={`w-1.5 h-1.5 rounded-full ${isBuyCall ? 'bg-[#19E58C]' : 'bg-[#FF4D67]'} animate-ping`} />
              <span>HIGH CONVICTION TRADE ALERT (SEMI-AUTO)</span>
            </div>
            <h2 className="text-xl font-black text-white font-mono flex items-center gap-2">
              <span className={actionColor}>
                {alert.recommendedAction} {alert.recommendedStrike}
              </span>
              <span className="text-xs px-2 py-0.5 rounded bg-[#06080C] text-slate-300 font-mono font-bold border border-[#1A212D]">
                SCORE: {alert.totalScore > 0 ? `+${alert.totalScore.toFixed(0)}` : alert.totalScore.toFixed(0)}
              </span>
            </h2>
          </div>
        </div>

        {/* Setup Rationale */}
        <div className="bg-[#06080C]/90 p-3.5 rounded-xl border border-[#1A212D] mb-4 text-xs font-mono text-slate-300 leading-relaxed relative z-10">
          {alert.setupRationale}
        </div>

        {/* Trade Details Grid */}
        <div className="grid grid-cols-2 gap-2.5 font-mono mb-4 text-xs relative z-10">
          <div className="bg-[#06080C] p-2.5 rounded-lg border border-[#1A212D] ob-hover-lift">
            <span className="text-[10px] text-slate-400">Index Spot:</span>
            <div className="font-bold text-white text-sm">₹{alert.spotPrice.toFixed(2)}</div>
          </div>
          <div className="bg-[#06080C] p-2.5 rounded-lg border border-[#1A212D] ob-hover-lift">
            <span className="text-[10px] text-slate-400">Est. Entry Premium:</span>
            <div className="font-bold text-[#45B8FF] text-sm">
              ₹{alert.recommendedEntryPrice > 0 ? alert.recommendedEntryPrice.toFixed(2) : '120.00'}
            </div>
          </div>
          <div className="bg-[#06080C] p-2.5 rounded-lg border border-[#1A212D] ob-hover-lift">
            <span className="text-[10px] text-slate-400">Stop-Loss Target:</span>
            <div className="font-bold text-[#FF4D67] text-sm">
              ₹{alert.recommendedStopLoss > 0 ? alert.recommendedStopLoss.toFixed(2) : '90.00'} (-25%)
            </div>
          </div>
          <div className="bg-[#06080C] p-2.5 rounded-lg border border-[#1A212D] ob-hover-lift">
            <span className="text-[10px] text-slate-400">Profit Target:</span>
            <div className="font-bold text-[#19E58C] text-sm">
              ₹{alert.recommendedTarget > 0 ? alert.recommendedTarget.toFixed(2) : '174.00'} (+45%)
            </div>
          </div>
        </div>

        {/* Lot Sizing Stepper */}
        <div className="bg-[#06080C] p-3 rounded-xl border border-[#1A212D] flex items-center justify-between mb-4 relative z-10">
          <span className="text-xs font-mono text-slate-300 font-semibold">Lots to Execute:</span>
          <div className="flex items-center space-x-2">
            {[1, 2, 5, 10].map((l) => (
              <button
                key={l}
                onClick={() => setLots(l)}
                className={`px-3 py-1 rounded text-xs font-mono font-bold transition ob-hover-lift ${
                  lots === l
                    ? 'bg-gradient-to-r from-[#0284C7] to-[#0EA5E9] text-white shadow-[0_0_10px_rgba(14,165,233,0.4)]'
                    : 'bg-[#11161F] text-slate-400 hover:text-white border border-[#1A212D]'
                }`}
              >
                {l} Lot{l > 1 ? 's' : ''}
              </button>
            ))}
          </div>
        </div>

        {/* Safety Lock Checkbox */}
        <label className="flex items-center space-x-2.5 text-xs text-slate-300 cursor-pointer mb-5 p-2.5 rounded-xl bg-[#06080C] border border-[#1A212D] relative z-10">
          <input
            type="checkbox"
            checked={confirmedSafety}
            onChange={(e) => setConfirmedSafety(e.target.checked)}
            className="w-4 h-4 rounded bg-[#11161F] border-[#1A212D] text-[#45B8FF] focus:ring-[#45B8FF] cursor-pointer"
          />
          <span className="font-mono text-[11px]">I confirm this execution setup and authorize live broker order placement.</span>
        </label>

        {/* Action Buttons */}
        <div className="flex space-x-3 relative z-10">
          <button
            onClick={onDismiss}
            disabled={isExecuting}
            className="flex-1 py-2.5 bg-[#1A212D] hover:bg-[#242D3C] text-slate-300 font-mono text-xs font-bold rounded-xl transition ob-hover-lift"
          >
            Dismiss Alert
          </button>
          <button
            onClick={() => onConfirm(lots)}
            disabled={!confirmedSafety || isExecuting}
            className={`flex-1 py-2.5 text-white font-mono text-xs font-extrabold rounded-xl transition flex items-center justify-center space-x-2 shadow-xl ${
              confirmedSafety && !isExecuting
                ? isBuyCall ? 'ob-btn-bull' : 'ob-btn-bear'
                : 'bg-[#11161F] text-slate-600 cursor-not-allowed border border-[#1A212D]'
            }`}
          >
            <ShieldCheck className="w-4 h-4" />
            <span>{isExecuting ? 'DISPATCHING...' : 'CONFIRM LIVE ORDER'}</span>
          </button>
        </div>
      </div>
    </div>
  );
};
