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
      <div className="bg-slate-900 border border-slate-700 rounded-2xl max-w-lg w-full p-6 shadow-2xl relative animate-in fade-in zoom-in duration-200">
        <button
          onClick={onDismiss}
          className="absolute top-4 right-4 text-slate-400 hover:text-white transition"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Top Header */}
        <div className="flex items-center space-x-3 mb-4">
          <div
            className={`w-10 h-10 rounded-xl flex items-center justify-center ${
              isBuyCall ? 'bg-emerald-500/20 text-emerald-400' : 'bg-rose-500/20 text-rose-400'
            }`}
          >
            <Zap className="w-6 h-6 fill-current" />
          </div>
          <div>
            <div className="text-[10px] font-mono uppercase tracking-wider text-slate-400">
              HIGH CONVICTION TRADE ALERT (SEMI-AUTO)
            </div>
            <h2 className="text-xl font-black text-white font-mono flex items-center gap-2">
              <span className={actionColor}>
                {alert.recommendedAction} {alert.recommendedStrike}
              </span>
              <span className="text-xs px-2 py-0.5 rounded bg-slate-800 text-slate-300 font-mono font-bold">
                SCORE: {alert.totalScore > 0 ? `+${alert.totalScore.toFixed(0)}` : alert.totalScore.toFixed(0)}
              </span>
            </h2>
          </div>
        </div>

        {/* Setup Rationale */}
        <div className="bg-slate-950 p-3.5 rounded-xl border border-slate-800 mb-4 text-xs font-mono text-slate-300 leading-relaxed">
          {alert.setupRationale}
        </div>

        {/* Trade Details Grid */}
        <div className="grid grid-cols-2 gap-2.5 font-mono mb-4 text-xs">
          <div className="bg-slate-950 p-2.5 rounded-lg border border-slate-800">
            <span className="text-[10px] text-slate-400">Index Spot:</span>
            <div className="font-bold text-white text-sm">₹{alert.spotPrice.toFixed(2)}</div>
          </div>
          <div className="bg-slate-950 p-2.5 rounded-lg border border-slate-800">
            <span className="text-[10px] text-slate-400">Est. Entry Premium:</span>
            <div className="font-bold text-cyan-400 text-sm">
              ₹{alert.recommendedEntryPrice > 0 ? alert.recommendedEntryPrice.toFixed(2) : '120.00'}
            </div>
          </div>
          <div className="bg-slate-950 p-2.5 rounded-lg border border-slate-800">
            <span className="text-[10px] text-slate-400">Stop-Loss Target:</span>
            <div className="font-bold text-rose-400 text-sm">
              ₹{alert.recommendedStopLoss > 0 ? alert.recommendedStopLoss.toFixed(2) : '90.00'} (-25%)
            </div>
          </div>
          <div className="bg-slate-950 p-2.5 rounded-lg border border-slate-800">
            <span className="text-[10px] text-slate-400">Profit Target:</span>
            <div className="font-bold text-emerald-400 text-sm">
              ₹{alert.recommendedTarget > 0 ? alert.recommendedTarget.toFixed(2) : '174.00'} (+45%)
            </div>
          </div>
        </div>

        {/* Lot Sizing Stepper */}
        <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 flex items-center justify-between mb-4">
          <span className="text-xs font-mono text-slate-300 font-semibold">Lots to Execute:</span>
          <div className="flex items-center space-x-2">
            {[1, 2, 5, 10].map((l) => (
              <button
                key={l}
                onClick={() => setLots(l)}
                className={`px-3 py-1 rounded text-xs font-mono font-bold transition ${
                  lots === l ? 'bg-cyan-600 text-white' : 'bg-slate-900 text-slate-400 hover:text-white'
                }`}
              >
                {l} Lot{l > 1 ? 's' : ''}
              </button>
            ))}
          </div>
        </div>

        {/* Safety Lock Checkbox */}
        <label className="flex items-center space-x-2.5 text-xs text-slate-300 cursor-pointer mb-5 p-2 rounded-lg bg-slate-950 border border-slate-800">
          <input
            type="checkbox"
            checked={confirmedSafety}
            onChange={(e) => setConfirmedSafety(e.target.checked)}
            className="w-4 h-4 rounded bg-slate-900 border-slate-700 text-cyan-600 focus:ring-cyan-500 cursor-pointer"
          />
          <span>I confirm this execution setup and authorize order placement.</span>
        </label>

        {/* Action Buttons */}
        <div className="flex space-x-3">
          <button
            onClick={onDismiss}
            disabled={isExecuting}
            className="flex-1 py-2.5 bg-slate-800 hover:bg-slate-700 text-slate-300 font-mono text-xs font-bold rounded-xl transition"
          >
            Dismiss Alert
          </button>
          <button
            onClick={() => onConfirm(lots)}
            disabled={!confirmedSafety || isExecuting}
            className={`flex-1 py-2.5 text-white font-mono text-xs font-extrabold rounded-xl transition flex items-center justify-center space-x-2 shadow-lg ${
              confirmedSafety && !isExecuting
                ? `${actionBg} shadow-cyan-500/20`
                : 'bg-slate-800 text-slate-500 cursor-not-allowed'
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
