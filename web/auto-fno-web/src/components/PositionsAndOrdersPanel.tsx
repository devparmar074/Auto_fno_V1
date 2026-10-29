import { AlertOctagon, CheckCircle2, Clock, ListOrdered, ShieldAlert, TrendingDown, TrendingUp, XCircle } from 'lucide-react';
import React, { useState } from 'react';
import type { PositionDto } from '../types/fnoTrading';

interface PositionsAndOrdersPanelProps {
  positions: PositionDto[];
  orders: any[];
  totalUnrealizedPnl: number;
  todayRealizedPnl: number;
  onSquareOff: (instrumentKey: string) => void;
  onSquareOffAll: () => void;
  isSquaringOff: boolean;
}

export const PositionsAndOrdersPanel: React.FC<PositionsAndOrdersPanelProps> = ({
  positions,
  orders,
  totalUnrealizedPnl,
  todayRealizedPnl,
  onSquareOff,
  onSquareOffAll,
  isSquaringOff,
}) => {
  const [activeTab, setActiveTab] = useState<'positions' | 'orders'>('positions');
  const [showSquareOffAllConfirm, setShowSquareOffAllConfirm] = useState(false);

  const totalDayPnl = totalUnrealizedPnl + todayRealizedPnl;
  const isProfitable = totalDayPnl >= 0;

  return (
    <div className="ob-panel rounded-2xl overflow-hidden shadow-2xl mb-6 border border-[#1A212D] relative">
      {/* Header and Summary Strip */}
      <div className="bg-[#06080C]/90 px-4 py-3 border-b border-[#1A212D] flex flex-wrap items-center justify-between gap-3 relative overflow-hidden">
        {/* Scanner Radar Beam in Positions Header */}
        <div className="ob-scanner-beam" />

        {/* Tabs */}
        <div className="flex items-center space-x-1.5 bg-[#06080C] p-0.5 rounded-lg border border-[#1A212D] relative z-10 shadow-inner">
          <button
            onClick={() => setActiveTab('positions')}
            className={`px-3.5 py-1 rounded-md text-xs font-bold font-mono transition flex items-center space-x-2 ob-hover-lift ${
              activeTab === 'positions'
                ? 'bg-gradient-to-r from-[#0284C7] to-[#0EA5E9] text-white shadow-[0_0_10px_rgba(14,165,233,0.4)]'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            <span>POSITIONS</span>
            <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-black/40 text-cyan-200 font-mono">
              {positions.length}
            </span>
          </button>
          <button
            onClick={() => setActiveTab('orders')}
            className={`px-3.5 py-1 rounded-md text-xs font-bold font-mono transition flex items-center space-x-2 ob-hover-lift ${
              activeTab === 'orders'
                ? 'bg-gradient-to-r from-[#6366F1] to-[#8B5CF6] text-white shadow-[0_0_10px_rgba(139,92,246,0.4)]'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            <span>TODAY'S ORDERS</span>
            <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-black/40 text-violet-200 font-mono">
              {orders.length}
            </span>
          </button>
        </div>

        {/* P&L Performance Stats */}
        <div className="flex items-center space-x-4 relative z-10">
          <div className="text-right">
            <span className="text-[10px] text-slate-400 font-mono uppercase">Unrealized MTM: </span>
            <span
              className={`text-xs font-mono font-bold ${
                totalUnrealizedPnl >= 0 ? 'text-[#19E58C]' : 'text-[#FF4D67]'
              }`}
            >
              {totalUnrealizedPnl >= 0 ? '+' : ''}₹
              {totalUnrealizedPnl.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </span>
          </div>

          <div className="text-right">
            <span className="text-[10px] text-slate-400 font-mono uppercase">Realized: </span>
            <span
              className={`text-xs font-mono font-bold ${
                todayRealizedPnl >= 0 ? 'text-[#19E58C]' : 'text-[#FF4D67]'
              }`}
            >
              {todayRealizedPnl >= 0 ? '+' : ''}₹
              {todayRealizedPnl.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </span>
          </div>

          <div className={`px-3.5 py-1 rounded-lg border flex items-center space-x-2.5 transition-all ${
            isProfitable
              ? 'bg-[#19E58C]/10 border-[#19E58C]/30 shadow-[0_0_10px_rgba(25,229,140,0.2)]'
              : 'bg-[#FF4D67]/10 border-[#FF4D67]/30 shadow-[0_0_10px_rgba(255,77,103,0.2)]'
          }`}>
            <span className="text-[10px] text-slate-400 font-mono uppercase font-bold">TOTAL P&L:</span>
            <span
              className={`text-sm font-mono font-black ${
                isProfitable ? 'text-[#19E58C]' : 'text-[#FF4D67]'
              }`}
            >
              {isProfitable ? '+' : ''}₹
              {totalDayPnl.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </span>
          </div>

          {/* Square Off All Button */}
          {positions.length > 0 && (
            <button
              onClick={() => setShowSquareOffAllConfirm(true)}
              disabled={isSquaringOff}
              className="px-3 py-1.5 ob-btn-bear text-white rounded-lg text-xs font-bold font-mono transition"
            >
              Square Off All
            </button>
          )}
        </div>
      </div>

      {/* Tab 1: Positions Table */}
      {activeTab === 'positions' && (
        <div className="overflow-x-auto">
          {positions.length === 0 ? (
            <div className="py-12 text-center text-slate-400 font-mono text-xs flex flex-col items-center justify-center gap-2">
              <span className="w-2 h-2 rounded-full bg-slate-600 animate-ping" />
              <span>No active open FnO positions. Orders executed in Manual, Semi-Auto, or Full-Auto will appear here live.</span>
            </div>
          ) : (
            <table className="w-full text-left text-xs font-mono border-collapse">
              <thead className="bg-[#06080C]/90 text-slate-400 text-[10px] border-b border-[#1A212D]">
                <tr>
                  <th className="py-2.5 px-3">INSTRUMENT / STRIKE</th>
                  <th className="py-2.5 px-3 text-center">TYPE</th>
                  <th className="py-2.5 px-3 text-right">LOTS / QTY</th>
                  <th className="py-2.5 px-3 text-right">AVG ENTRY</th>
                  <th className="py-2.5 px-3 text-right">CURRENT LTP</th>
                  <th className="py-2.5 px-3 text-right">UNREALIZED P&L</th>
                  <th className="py-2.5 px-3 text-right">RETURN %</th>
                  <th className="py-2.5 px-3 text-center">ACTION</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#1A212D]/60 bg-[#06080C]/30">
                {positions.map((p) => {
                  const isPnlPositive = p.unrealizedPnl >= 0;
                  return (
                    <tr key={p.instrumentKey} className="hover:bg-[#11161F]/80 transition ob-animate-rise">
                      <td className="py-2.5 px-3 font-bold text-white flex items-center gap-2">
                        <span className={`w-1.5 h-1.5 rounded-full ${isPnlPositive ? 'bg-[#19E58C]' : 'bg-[#FF4D67]'} animate-pulse`} />
                        {p.tradingSymbol || p.instrumentKey}
                      </td>
                      <td className="py-2.5 px-3 text-center">
                        <span
                          className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                            p.optionType === 'CE'
                              ? 'bg-[#19E58C]/15 text-[#19E58C] border border-[#19E58C]/40'
                              : 'bg-[#FF4D67]/15 text-[#FF4D67] border border-[#FF4D67]/40'
                          }`}
                        >
                          {p.optionType || 'OPT'}
                        </span>
                      </td>
                      <td className="py-2.5 px-3 text-right text-slate-300">
                        {p.lots} Lot ({p.quantity} Qty)
                      </td>
                      <td className="py-2.5 px-3 text-right text-slate-300">
                        ₹{p.averagePrice.toFixed(2)}
                      </td>
                      <td className="py-2.5 px-3 text-right font-bold text-white">
                        ₹{p.currentLtp.toFixed(2)}
                      </td>
                      <td
                        className={`py-2.5 px-3 text-right font-extrabold ${
                          isPnlPositive ? 'text-[#19E58C]' : 'text-[#FF4D67]'
                        }`}
                      >
                        {isPnlPositive ? '+' : ''}₹{p.unrealizedPnl.toFixed(2)}
                      </td>
                      <td
                        className={`py-2.5 px-3 text-right font-bold ${
                          isPnlPositive ? 'text-[#19E58C]' : 'text-[#FF4D67]'
                        }`}
                      >
                        {isPnlPositive ? '+' : ''}{p.pnlPercent.toFixed(1)}%
                      </td>
                      <td className="py-2.5 px-3 text-center">
                        <button
                          onClick={() => onSquareOff(p.instrumentKey)}
                          disabled={isSquaringOff}
                          className="px-2.5 py-1 ob-btn-bear text-white rounded text-[10px] font-bold transition shadow-sm"
                        >
                          Exit
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* Tab 2: Orders Table */}
      {activeTab === 'orders' && (
        <div className="overflow-x-auto max-h-[300px]">
          {orders.length === 0 ? (
            <div className="py-12 text-center text-slate-400 font-mono text-xs">
              No orders placed today yet.
            </div>
          ) : (
            <table className="w-full text-left text-xs font-mono border-collapse">
              <thead className="bg-[#06080C]/90 text-slate-400 text-[10px] border-b border-[#1A212D] sticky top-0">
                <tr>
                  <th className="py-2.5 px-3">TIME (UTC)</th>
                  <th className="py-2.5 px-3">ORDER ID</th>
                  <th className="py-2.5 px-3">SYMBOL</th>
                  <th className="py-2.5 px-3 text-center">ACTION</th>
                  <th className="py-2.5 px-3 text-right">QTY (LOTS)</th>
                  <th className="py-2.5 px-3 text-right">FILLED PRICE</th>
                  <th className="py-2.5 px-3 text-center">MODE</th>
                  <th className="py-2.5 px-3 text-center">STATUS</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#1A212D]/60 bg-[#06080C]/30">
                {orders.map((o: any) => (
                  <tr key={o.id || o.correlationId} className="hover:bg-[#11161F]/80 transition ob-animate-rise">
                    <td className="py-2 px-3 text-slate-400">
                      {o.createdAtUtc ? new Date(o.createdAtUtc).toLocaleTimeString() : '—'}
                    </td>
                    <td className="py-2 px-3 text-slate-300">
                      {o.brokerOrderId || o.correlationId?.slice(0, 10) || o.id}
                    </td>
                    <td className="py-2 px-3 font-bold text-white">{o.tradingSymbol}</td>
                    <td className="py-2 px-3 text-center">
                      <span
                        className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                          o.transactionType === 0 || o.transactionType === 'BUY'
                            ? 'ob-buildup-long'
                            : 'ob-buildup-short'
                        }`}
                      >
                        {o.transactionType === 0 ? 'BUY' : o.transactionType === 1 ? 'SELL' : o.transactionType}
                      </span>
                    </td>
                    <td className="py-2 px-3 text-right text-slate-300">
                      {o.quantity} ({o.lots}L)
                    </td>
                    <td className="py-2 px-3 text-right font-bold text-white">
                      ₹{o.executedPrice > 0 ? o.executedPrice.toFixed(2) : o.price?.toFixed(2) || '0.00'}
                    </td>
                    <td className="py-2 px-3 text-center text-slate-400 text-[10px]">
                      {o.placedByMode === 2 ? 'FullAuto' : o.placedByMode === 1 ? 'SemiAuto' : 'Manual'}
                    </td>
                    <td className="py-2 px-3 text-center">
                      <span
                        className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                          o.status === 2 || o.status === 'Complete'
                            ? 'bg-[#19E58C]/15 text-[#19E58C] border border-[#19E58C]/40'
                            : o.status === 3 || o.status === 'Rejected'
                            ? 'bg-[#FF4D67]/15 text-[#FF4D67] border border-[#FF4D67]/40'
                            : 'bg-[#1A212D] text-slate-300'
                        }`}
                      >
                        {o.status === 2 ? 'COMPLETE' : o.status === 3 ? 'REJECTED' : o.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* Confirmation Modal for Square Off All */}
      {showSquareOffAllConfirm && (
        <div className="fixed inset-0 bg-black/85 backdrop-blur-md z-50 flex items-center justify-center p-4">
          <div className="ob-panel border border-red-500/50 rounded-2xl max-w-sm w-full p-6 text-center shadow-2xl ob-glow-bear">
            <div className="w-12 h-12 bg-red-500/10 text-red-400 rounded-full flex items-center justify-center mx-auto mb-3 border border-red-500/30">
              <AlertOctagon className="w-6 h-6 animate-bounce" />
            </div>
            <h3 className="text-base font-bold text-white mb-2 font-mono">Liquidate All Open Positions?</h3>
            <p className="text-xs text-slate-400 mb-5 leading-relaxed">
              This will immediately send market exit orders to square off all {positions.length} active positions.
            </p>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowSquareOffAllConfirm(false)}
                className="flex-1 py-2 bg-[#1A212D] hover:bg-[#242D3C] text-slate-300 text-xs font-semibold rounded-lg transition"
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  onSquareOffAll();
                  setShowSquareOffAllConfirm(false);
                }}
                className="flex-1 py-2 ob-btn-bear text-white text-xs font-bold rounded-lg transition"
              >
                Yes, Square Off All
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
