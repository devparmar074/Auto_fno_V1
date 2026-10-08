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
    <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden shadow-xl mb-6">
      {/* Header and Summary Strip */}
      <div className="bg-slate-950 px-4 py-2.5 border-b border-slate-800 flex flex-wrap items-center justify-between gap-3">
        {/* Tabs */}
        <div className="flex items-center space-x-1 bg-slate-900 p-0.5 rounded-lg border border-slate-800">
          <button
            onClick={() => setActiveTab('positions')}
            className={`px-3 py-1 rounded-md text-xs font-bold font-mono transition flex items-center space-x-1.5 ${
              activeTab === 'positions'
                ? 'bg-slate-800 text-white shadow'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            <span>POSITIONS</span>
            <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-slate-700 text-slate-200">
              {positions.length}
            </span>
          </button>
          <button
            onClick={() => setActiveTab('orders')}
            className={`px-3 py-1 rounded-md text-xs font-bold font-mono transition flex items-center space-x-1.5 ${
              activeTab === 'orders'
                ? 'bg-slate-800 text-white shadow'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            <span>TODAY'S ORDERS</span>
            <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-slate-700 text-slate-200">
              {orders.length}
            </span>
          </button>
        </div>

        {/* P&L Performance Stats */}
        <div className="flex items-center space-x-4">
          <div className="text-right">
            <span className="text-[10px] text-slate-400 font-mono uppercase">Unrealized MTM: </span>
            <span
              className={`text-xs font-mono font-bold ${
                totalUnrealizedPnl >= 0 ? 'text-emerald-400' : 'text-rose-400'
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
                todayRealizedPnl >= 0 ? 'text-emerald-400' : 'text-rose-400'
              }`}
            >
              {todayRealizedPnl >= 0 ? '+' : ''}₹
              {todayRealizedPnl.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </span>
          </div>

          <div className="bg-slate-900 px-3 py-1 rounded-lg border border-slate-800 flex items-center space-x-2">
            <span className="text-[10px] text-slate-400 font-mono uppercase font-bold">TOTAL P&L:</span>
            <span
              className={`text-sm font-mono font-black ${
                isProfitable ? 'text-emerald-400' : 'text-rose-400'
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
              className="px-3 py-1 bg-rose-950 hover:bg-rose-900 border border-rose-500/40 text-rose-300 rounded text-xs font-bold font-mono transition"
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
            <div className="py-12 text-center text-slate-400 font-mono text-xs">
              No active open FnO positions. Orders executed in Manual, Semi-Auto, or Full-Auto will appear here live.
            </div>
          ) : (
            <table className="w-full text-left text-xs font-mono border-collapse">
              <thead className="bg-slate-950/60 text-slate-400 text-[10px] border-b border-slate-800">
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
              <tbody className="divide-y divide-slate-800">
                {positions.map((p) => {
                  const isPnlPositive = p.unrealizedPnl >= 0;
                  return (
                    <tr key={p.instrumentKey} className="hover:bg-slate-800/40 transition">
                      <td className="py-2.5 px-3 font-bold text-white">
                        {p.tradingSymbol || p.instrumentKey}
                      </td>
                      <td className="py-2.5 px-3 text-center">
                        <span
                          className={`px-2 py-0.5 rounded text-[10px] font-bold ${
                            p.optionType === 'CE'
                              ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                              : 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
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
                          isPnlPositive ? 'text-emerald-400' : 'text-rose-400'
                        }`}
                      >
                        {isPnlPositive ? '+' : ''}₹{p.unrealizedPnl.toFixed(2)}
                      </td>
                      <td
                        className={`py-2.5 px-3 text-right font-bold ${
                          isPnlPositive ? 'text-emerald-400' : 'text-rose-400'
                        }`}
                      >
                        {isPnlPositive ? '+' : ''}{p.pnlPercent.toFixed(1)}%
                      </td>
                      <td className="py-2.5 px-3 text-center">
                        <button
                          onClick={() => onSquareOff(p.instrumentKey)}
                          disabled={isSquaringOff}
                          className="px-2 py-1 bg-red-600/80 hover:bg-red-500 text-white rounded text-[10px] font-bold transition"
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
              <thead className="bg-slate-950/60 text-slate-400 text-[10px] border-b border-slate-800 sticky top-0">
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
              <tbody className="divide-y divide-slate-800">
                {orders.map((o: any) => (
                  <tr key={o.id || o.correlationId} className="hover:bg-slate-800/40 transition">
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
                            ? 'bg-emerald-500/20 text-emerald-400'
                            : 'bg-rose-500/20 text-rose-400'
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
                            ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                            : o.status === 3 || o.status === 'Rejected'
                            ? 'bg-rose-500/10 text-rose-400 border border-rose-500/20'
                            : 'bg-slate-800 text-slate-300'
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
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-red-500/40 rounded-xl max-w-sm w-full p-6 text-center shadow-2xl">
            <div className="w-12 h-12 bg-red-500/10 text-red-400 rounded-full flex items-center justify-center mx-auto mb-3">
              <AlertOctagon className="w-6 h-6" />
            </div>
            <h3 className="text-base font-bold text-white mb-2">Liquidate All Open Positions?</h3>
            <p className="text-xs text-slate-400 mb-5 leading-relaxed">
              This will immediately send market exit orders to square off all {positions.length} active positions.
            </p>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowSquareOffAllConfirm(false)}
                className="flex-1 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold rounded-lg transition"
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  onSquareOffAll();
                  setShowSquareOffAllConfirm(false);
                }}
                className="flex-1 py-2 bg-red-600 hover:bg-red-500 text-white text-xs font-bold rounded-lg transition"
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
