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
    <header className="relative bg-[#0D1118]/90 backdrop-blur-xl border-b border-[#1A212D] sticky top-0 z-40 px-4 py-2.5 overflow-hidden">
      {/* Scanner Radar Beam */}
      <div className="ob-scanner-beam" />

      <div className="max-w-[1720px] mx-auto flex flex-wrap items-center justify-between gap-3 relative z-10">
        {/* Brand & Index Switcher */}
        <div className="flex items-center space-x-4">
          <div className="flex items-center space-x-2.5 group cursor-pointer">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-[#2DD4BF] via-[#45B8FF] to-[#7B7BFF] p-[1px] shadow-lg shadow-cyan-500/20 ob-hover-lift">
              <div className="w-full h-full bg-[#06080C] rounded-lg flex items-center justify-center">
                <Zap className="w-4 h-4 text-[#2DD4BF] fill-current animate-pulse" />
              </div>
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="font-extrabold text-base tracking-tight ob-grad-brand">AutoTrade</span>
                <span className="bg-[#45B8FF]/10 text-[#45B8FF] border border-[#45B8FF]/30 text-[10px] font-bold px-1.5 py-0.5 rounded uppercase tracking-wider shadow-sm">
                  FnO QUANTUM
                </span>
              </div>
              <div className="text-[10px] text-slate-400 font-mono tracking-wider">NIFTY 50 & SENSEX ENGINE</div>
            </div>
          </div>

          {/* Index Selector Tabs */}
          <div className="bg-[#06080C] p-0.5 rounded-lg border border-[#1A212D] flex items-center shadow-inner">
            <button
              onClick={() => onSelectIndex('NIFTY50')}
              className={`px-3 py-1 rounded-md text-xs font-bold font-mono transition flex items-center space-x-1.5 ob-hover-lift ${
                activeIndex === 'NIFTY50'
                  ? 'bg-gradient-to-r from-[#0284C7] to-[#0EA5E9] text-white shadow-[0_0_12px_rgba(14,165,233,0.4)] border border-cyan-400/30'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <span>NIFTY 50</span>
              <span className="text-[10px] px-1 rounded bg-black/40 font-mono text-cyan-200">NSE</span>
            </button>
            <button
              onClick={() => onSelectIndex('SENSEX')}
              className={`px-3 py-1 rounded-md text-xs font-bold font-mono transition flex items-center space-x-1.5 ob-hover-lift ${
                activeIndex === 'SENSEX'
                  ? 'bg-gradient-to-r from-[#6366F1] to-[#8B5CF6] text-white shadow-[0_0_12px_rgba(139,92,246,0.4)] border border-violet-400/30'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <span>SENSEX</span>
              <span className="text-[10px] px-1 rounded bg-black/40 font-mono text-violet-200">BSE</span>
            </button>
          </div>
        </div>

        {/* Live Spot Ticker */}
        <div className="flex items-center space-x-4">
          <div
            className={`px-3.5 py-1.5 rounded-lg border flex items-center space-x-3 transition-all duration-300 ob-hover-lift ${
              isMarketOpen && isTickUp
                ? 'bg-[#19E58C]/10 border-[#19E58C]/60 ob-flash-up shadow-[0_0_15px_rgba(25,229,140,0.25)]'
                : isMarketOpen && isTickDown
                ? 'bg-[#FF4D67]/10 border-[#FF4D67]/60 ob-flash-down shadow-[0_0_15px_rgba(255,77,103,0.25)]'
                : 'bg-[#06080C]/80 border-[#1A212D]'
            }`}
          >
            <div>
              <div className="text-[10px] font-mono text-slate-400 uppercase tracking-wider flex items-center gap-2">
                <span>{activeIndex} SPOT</span>
                {isMarketOpen ? (
                  <span className="ob-sonar-dot" title="Market Live Feed Active" />
                ) : (
                  <span className="w-1.5 h-1.5 rounded-full bg-slate-500" title="Market Closed — Static Closing Price" />
                )}
              </div>
              <div className="flex items-baseline space-x-2.5">
                <span className="text-lg font-black font-mono tracking-tight text-white drop-shadow-sm">
                  ₹{spotPrice.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
                <span
                  className={`text-xs font-mono font-bold ${
                    isPositive ? 'text-[#19E58C]' : 'text-[#FF4D67]'
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
          <div className="flex items-center space-x-1.5 bg-[#06080C] px-2.5 py-1.5 rounded-lg border border-[#1A212D] ob-hover-lift">
            <span className="text-[11px] font-medium text-slate-400">Expiry:</span>
            <select
              value={selectedExpiry}
              onChange={(e) => onSelectExpiry(e.target.value)}
              className="bg-transparent text-white text-xs font-mono font-semibold focus:outline-none cursor-pointer pr-1"
            >
              {availableExpiries.map((exp) => (
                <option key={exp} value={exp} className="bg-[#0D1118] text-white">
                  {exp}
                </option>
              ))}
            </select>
          </div>

          {/* Market Status Pill */}
          <div
            className={`px-3 py-1 rounded-full border text-[11px] font-bold flex items-center space-x-2 font-mono transition-all ${
              isMarketOpen
                ? 'bg-[#19E58C]/10 border-[#19E58C]/30 text-[#19E58C] shadow-[0_0_10px_rgba(25,229,140,0.2)]'
                : 'bg-[#FF4D67]/10 border-[#FF4D67]/30 text-[#FF4D67]'
            }`}
            title={isMarketOpen ? 'NSE/BSE Exchange is Open (09:15 - 15:30 IST)' : 'NSE/BSE Exchange is Closed (Weekends / Holidays / After Hours)'}
          >
            {isMarketOpen ? (
              <span className="ob-sonar-dot" />
            ) : (
              <span className="w-2 h-2 rounded-full bg-[#FF4D67]" />
            )}
            <span>{isMarketOpen ? 'MARKET LIVE' : 'MARKET CLOSED'}</span>
          </div>
        </div>

        {/* Right Controls: Bot Mode, Kill Switch, Broker Auth */}
        <div className="flex items-center space-x-3">
          {/* Bot Mode Switcher */}
          <div className="bg-[#06080C] p-0.5 rounded-lg border border-[#1A212D] flex items-center shadow-inner">
            <button
              onClick={() => handleModeClick('Manual')}
              className={`px-2.5 py-1 rounded-md text-[11px] font-bold transition ob-hover-lift ${
                botMode === 'Manual'
                  ? 'bg-[#1A212D] text-white shadow-sm border border-slate-700/50'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              Manual
            </button>
            <button
              onClick={() => handleModeClick('SemiAuto')}
              className={`px-2.5 py-1 rounded-md text-[11px] font-bold transition flex items-center space-x-1.5 ob-hover-lift ${
                botMode === 'SemiAuto'
                  ? 'bg-gradient-to-r from-amber-600 to-amber-500 text-white shadow-[0_0_12px_rgba(245,158,11,0.35)]'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <span>Semi-Auto</span>
              <span className="ob-sonar-dot-amber" />
            </button>
            <button
              onClick={() => handleModeClick('FullAuto')}
              className={`px-2.5 py-1 rounded-md text-[11px] font-bold transition flex items-center space-x-1.5 ob-hover-lift ${
                botMode === 'FullAuto'
                  ? 'bg-gradient-to-r from-emerald-600 to-emerald-500 text-white shadow-[0_0_12px_rgba(16,185,129,0.35)] ob-glow-bull'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <span>Full-Auto</span>
              <span className="ob-sonar-dot" />
            </button>
          </div>

          {/* Emergency Kill Switch Button */}
          <button
            onClick={onTriggerKillSwitch}
            className={`px-3 py-1.5 rounded-lg text-xs font-bold font-mono transition flex items-center space-x-1.5 shadow-lg ob-hover-lift ${
              isKillSwitchActive
                ? 'bg-red-950 border border-red-500 text-red-400 animate-pulse'
                : 'ob-btn-bear text-white'
            }`}
            title="Immediately disarm all bots and square off open positions"
          >
            <AlertOctagon className="w-3.5 h-3.5" />
            <span>{isKillSwitchActive ? 'DISARMED' : 'KILL SWITCH'}</span>
          </button>

          {/* Upstox Connect Pill */}
          <button
            onClick={onOpenAuthModal}
            className={`px-3 py-1.5 rounded-lg border text-xs font-semibold font-mono flex items-center space-x-2 transition ob-hover-lift ${
              authStatus.isConnected
                ? 'bg-[#19E58C]/10 border-[#19E58C]/40 text-[#19E58C] shadow-[0_0_10px_rgba(25,229,140,0.2)]'
                : 'bg-[#11161F] hover:bg-[#161C27] border-[#1A212D] text-slate-300'
            }`}
          >
            {authStatus.isConnected ? (
              <span className="ob-sonar-dot" />
            ) : (
              <Power className="w-3.5 h-3.5 text-amber-400" />
            )}
            <span>{authStatus.isConnected ? authStatus.userId || 'UPSTOX LIVE' : 'CONNECT UPSTOX'}</span>
          </button>

          {/* Refresh button */}
          <button
            onClick={onRefresh}
            disabled={isRefreshing}
            className="p-1.5 bg-[#11161F] hover:bg-[#161C27] text-slate-300 rounded-lg border border-[#1A212D] transition ob-hover-lift"
            title="Refresh Data"
          >
            <RefreshCw className={`w-4 h-4 ${isRefreshing ? 'animate-spin text-[#45B8FF]' : ''}`} />
          </button>
        </div>
      </div>

      {/* Full Auto Confirmation Dialog */}
      {showModeConfirm && (
        <div className="fixed inset-0 bg-black/85 backdrop-blur-md z-50 flex items-center justify-center p-4">
          <div className="ob-panel border border-amber-500/50 rounded-2xl max-w-md w-full p-6 shadow-2xl ob-glow-brand">
            <div className="w-12 h-12 bg-amber-500/10 text-amber-400 rounded-full flex items-center justify-center mx-auto mb-3 border border-amber-500/30">
              <ShieldAlert className="w-6 h-6" />
            </div>
            <h3 className="text-lg font-bold text-white text-center mb-1">
              Enable Full-Auto Algorithmic Execution?
            </h3>
            <p className="text-xs text-slate-300 text-center mb-4 leading-relaxed">
              In Full-Auto mode, orders will be dispatched directly to your broker without manual confirmation whenever quantitative conviction crosses the ±60 threshold.
            </p>
            <div className="bg-[#06080C] p-3 rounded-lg border border-[#1A212D] text-xs text-slate-400 space-y-1 font-mono mb-5">
              <div>• Default lot size: 1 Lot</div>
              <div>• Hard Stop-Loss: 25% of premium</div>
              <div>• Take-Profit Target: 45% of premium</div>
              <div>• Daily Max Loss Cutoff: ₹5,000</div>
              <div>• Auto Square-Off: 03:15 PM IST</div>
            </div>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowModeConfirm(null)}
                className="flex-1 py-2 bg-[#1A212D] hover:bg-[#242D3C] text-slate-300 text-xs font-semibold rounded-lg transition"
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  onSelectBotMode('FullAuto');
                  setShowModeConfirm(null);
                }}
                className="flex-1 py-2 ob-btn-bull text-white text-xs font-bold rounded-lg transition"
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
