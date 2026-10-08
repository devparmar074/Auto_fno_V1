import { Activity, AlertOctagon, CheckCircle2, ChevronDown, Lock, Power, RefreshCw, ShieldAlert, Zap } from 'lucide-react';
import React, { useState } from 'react';
import type { BotMode, IndexSymbol, UpstoxAuthStatusDto } from '../types/fnoTrading';

interface HeaderBarProps {
  activeIndex: IndexSymbol;
  onSelectIndex: (index: IndexSymbol) => void;
  spotPrice: number;
  prevSpotPrice: number;
  spotChange: number;
  spotChangePercent: number;
  isTickUp: boolean;
  isTickDown: boolean;
  availableExpiries: string[];
  selectedExpiry: string;
  onSelectExpiry: (exp: string) => void;
  marketStatus: string;
  botMode: BotMode;
  onSelectBotMode: (mode: BotMode) => void;
  isKillSwitchActive: boolean;
  onTriggerKillSwitch: () => void;
  authStatus: UpstoxAuthStatusDto;
  onOpenAuthModal: () => void;
  onRefresh: () => void;
  isRefreshing: boolean;
}

export const HeaderBar: React.FC<HeaderBarProps> = ({
  activeIndex,
  onSelectIndex,
  spotPrice,
  spotChange,
  spotChangePercent,
  isTickUp,
  isTickDown,
  availableExpiries,
  selectedExpiry,
  onSelectExpiry,
  marketStatus,
  botMode,
  onSelectBotMode,
  isKillSwitchActive,
  onTriggerKillSwitch,
  authStatus,
  onOpenAuthModal,
  onRefresh,
  isRefreshing,
}) => {
  const [showModeConfirm, setShowModeConfirm] = useState<BotMode | null>(null);

  const handleModeClick = (mode: BotMode) => {
    if (mode === 'FullAuto') {
      setShowModeConfirm(mode);
    } else {
      onSelectBotMode(mode);
    }
  };

  const isMarketOpen = marketStatus === 'OPEN';
  const isPositive = spotChange >= 0;

  return (
    <header className="bg-slate-900/90 backdrop-blur-md border-b border-slate-800 sticky top-0 z-40 px-4 py-2.5">
      <div className="max-w-[1720px] mx-auto flex flex-wrap items-center justify-between gap-3">
        {/* Brand & Index Switcher */}
        <div className="flex items-center space-x-4">
          <div className="flex items-center space-x-2">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-cyan-600 to-emerald-500 flex items-center justify-center font-black text-slate-950 shadow-lg shadow-cyan-500/20">
              <Zap className="w-5 h-5 text-slate-950 fill-current" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-extrabold text-white text-base tracking-tight">AutoTrade</span>
                <span className="bg-cyan-500/20 text-cyan-400 border border-cyan-500/30 text-[10px] font-bold px-1.5 py-0.5 rounded uppercase">FnO</span>
              </div>
              <div className="text-[10px] text-slate-400 font-mono tracking-wider">NIFTY 50 & SENSEX ENGINE</div>
            </div>
          </div>

          {/* Index Selector Tabs */}
          <div className="bg-slate-950 p-0.5 rounded-lg border border-slate-800 flex items-center">
            <button
              onClick={() => onSelectIndex('NIFTY50')}
              className={`px-3 py-1 rounded-md text-xs font-bold transition flex items-center space-x-1.5 ${
                activeIndex === 'NIFTY50'
                  ? 'bg-cyan-600 text-white shadow-md shadow-cyan-600/30'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <span>NIFTY 50</span>
              <span className="text-[10px] px-1 rounded bg-black/30 font-mono">NSE</span>
            </button>
            <button
              onClick={() => onSelectIndex('SENSEX')}
              className={`px-3 py-1 rounded-md text-xs font-bold transition flex items-center space-x-1.5 ${
                activeIndex === 'SENSEX'
                  ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/30'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <span>SENSEX</span>
              <span className="text-[10px] px-1 rounded bg-black/30 font-mono">BSE</span>
            </button>
          </div>
        </div>

        {/* Live Spot Ticker */}
        <div className="flex items-center space-x-4">
          <div
            className={`px-3.5 py-1.5 rounded-lg border flex items-center space-x-3 transition-colors duration-300 ${
              isMarketOpen && isTickUp
                ? 'bg-emerald-500/20 border-emerald-500/50 tick-up'
                : isMarketOpen && isTickDown
                ? 'bg-rose-500/20 border-rose-500/50 tick-down'
                : 'bg-slate-950/70 border-slate-800'
            }`}
          >
            <div>
              <div className="text-[10px] font-mono text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
                <span>{activeIndex} SPOT</span>
                {isMarketOpen ? (
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping" title="Market Live Feed" />
                ) : (
                  <span className="w-1.5 h-1.5 rounded-full bg-slate-500" title="Market Closed — Static Closing Price" />
                )}
              </div>
              <div className="flex items-baseline space-x-2">
                <span className="text-lg font-black font-mono tracking-tight text-white">
                  ₹{spotPrice.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
                <span
                  className={`text-xs font-mono font-bold ${
                    isPositive ? 'text-emerald-400' : 'text-rose-400'
                  }`}
                >
                  {isPositive ? '+' : ''}
                  {spotChange.toFixed(2)} ({isPositive ? '+' : ''}
                  {spotChangePercent.toFixed(2)}%)
                </span>
              </div>
            </div>
          </div>

          {/* Expiry Dropdown */}
          <div className="flex items-center space-x-1.5 bg-slate-950 px-2.5 py-1.5 rounded-lg border border-slate-800">
            <span className="text-[11px] font-medium text-slate-400">Expiry:</span>
            <select
              value={selectedExpiry}
              onChange={(e) => onSelectExpiry(e.target.value)}
              className="bg-transparent text-white text-xs font-mono font-semibold focus:outline-none cursor-pointer pr-1"
            >
              {availableExpiries.map((exp) => (
                <option key={exp} value={exp} className="bg-slate-900 text-white">
                  {exp}
                </option>
              ))}
            </select>
          </div>

          {/* Market Status Pill */}
          <div
            className={`px-2.5 py-1 rounded-full border text-[11px] font-bold flex items-center space-x-1.5 font-mono ${
              isMarketOpen
                ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                : 'bg-rose-500/10 border-rose-500/30 text-rose-400'
            }`}
            title={isMarketOpen ? 'NSE/BSE Exchange is Open (09:15 - 15:30 IST)' : 'NSE/BSE Exchange is Closed (Weekends / Holidays / After Hours)'}
          >
            <span
              className={`w-2 h-2 rounded-full ${
                isMarketOpen ? 'bg-emerald-400 animate-pulse' : 'bg-rose-400'
              }`}
            />
            <span>{isMarketOpen ? 'MARKET LIVE' : 'MARKET CLOSED'}</span>
          </div>
        </div>

        {/* Right Controls: Bot Mode, Kill Switch, Broker Auth */}
        <div className="flex items-center space-x-3">
          {/* Bot Mode Switcher */}
          <div className="bg-slate-950 p-0.5 rounded-lg border border-slate-800 flex items-center">
            <button
              onClick={() => handleModeClick('Manual')}
              className={`px-2.5 py-1 rounded-md text-[11px] font-bold transition ${
                botMode === 'Manual'
                  ? 'bg-slate-800 text-white shadow'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              Manual
            </button>
            <button
              onClick={() => handleModeClick('SemiAuto')}
              className={`px-2.5 py-1 rounded-md text-[11px] font-bold transition flex items-center space-x-1 ${
                botMode === 'SemiAuto'
                  ? 'bg-amber-600 text-white shadow-md shadow-amber-600/20'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <span>Semi-Auto</span>
              <span className="w-1.5 h-1.5 rounded-full bg-amber-300 animate-pulse" />
            </button>
            <button
              onClick={() => handleModeClick('FullAuto')}
              className={`px-2.5 py-1 rounded-md text-[11px] font-bold transition flex items-center space-x-1 ${
                botMode === 'FullAuto'
                  ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/30'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <span>Full-Auto</span>
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-300 animate-pulse" />
            </button>
          </div>

          {/* Emergency Kill Switch Button */}
          <button
            onClick={onTriggerKillSwitch}
            className={`px-3 py-1.5 rounded-lg text-xs font-bold font-mono transition flex items-center space-x-1.5 shadow-lg ${
              isKillSwitchActive
                ? 'bg-red-950 border border-red-500 text-red-400 animate-pulse'
                : 'bg-red-600 hover:bg-red-700 text-white shadow-red-600/30'
            }`}
            title="Immediately disarm all bots and square off open positions"
          >
            <AlertOctagon className="w-3.5 h-3.5" />
            <span>{isKillSwitchActive ? 'DISARMED' : 'KILL SWITCH'}</span>
          </button>

          {/* Upstox Connect Pill */}
          <button
            onClick={onOpenAuthModal}
            className={`px-3 py-1.5 rounded-lg border text-xs font-semibold font-mono flex items-center space-x-1.5 transition ${
              authStatus.isConnected
                ? 'bg-emerald-950/40 border-emerald-500/40 text-emerald-300 hover:bg-emerald-900/40'
                : 'bg-slate-800 hover:bg-slate-700 border-slate-700 text-slate-200'
            }`}
          >
            <Power className="w-3.5 h-3.5" />
            <span>{authStatus.isConnected ? authStatus.userId || 'UPSTOX LIVE' : 'CONNECT UPSTOX'}</span>
          </button>

          {/* Refresh button */}
          <button
            onClick={onRefresh}
            disabled={isRefreshing}
            className="p-1.5 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg border border-slate-700 transition"
            title="Refresh Data"
          >
            <RefreshCw className={`w-4 h-4 ${isRefreshing ? 'animate-spin text-cyan-400' : ''}`} />
          </button>
        </div>
      </div>

      {/* Full Auto Confirmation Dialog */}
      {showModeConfirm && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-amber-500/40 rounded-xl max-w-md w-full p-6 shadow-2xl">
            <div className="w-12 h-12 bg-amber-500/10 text-amber-400 rounded-full flex items-center justify-center mx-auto mb-3">
              <ShieldAlert className="w-6 h-6" />
            </div>
            <h3 className="text-lg font-bold text-white text-center mb-1">
              Enable Full-Auto Algorithmic Execution?
            </h3>
            <p className="text-xs text-slate-300 text-center mb-4 leading-relaxed">
              In Full-Auto mode, orders will be dispatched directly to your broker without manual confirmation whenever quantitative conviction crosses the ±60 threshold.
            </p>
            <div className="bg-slate-950 p-3 rounded-lg border border-slate-800 text-xs text-slate-400 space-y-1 font-mono mb-5">
              <div>• Default lot size: 1 Lot</div>
              <div>• Hard Stop-Loss: 25% of premium</div>
              <div>• Take-Profit Target: 45% of premium</div>
              <div>• Daily Max Loss Cutoff: ₹5,000</div>
              <div>• Auto Square-Off: 03:15 PM IST</div>
            </div>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowModeConfirm(null)}
                className="flex-1 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold rounded-lg transition"
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  onSelectBotMode('FullAuto');
                  setShowModeConfirm(null);
                }}
                className="flex-1 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold rounded-lg transition shadow-lg shadow-emerald-600/30"
              >
                I Understand, Enable
              </button>
            </div>
          </div>
        </div>
      )}
    </header>
  );
};
