import { AlertTriangle, ShieldCheck, X } from 'lucide-react';
import React, { useState } from 'react';
import type { OptionType, PlaceFnoOrderRequest, StrikeDataDto } from '../types/fnoTrading';

interface OrderConfirmationModalProps {
  strike: StrikeDataDto;
  optionType: OptionType;
  action: 'BUY' | 'SELL';
  indexSymbol: string;
  onConfirm: (req: PlaceFnoOrderRequest) => void;
  onClose: () => void;
  isExecuting: boolean;
}

export const OrderConfirmationModal: React.FC<OrderConfirmationModalProps> = ({
  strike,
  optionType,
  action,
  indexSymbol,
  onConfirm,
  onClose,
  isExecuting,
}) => {
  const [lots, setLots] = useState(1);
  const [orderType, setOrderType] = useState<'MARKET' | 'LIMIT'>('MARKET');
  const [confirmedSafety, setConfirmedSafety] = useState(false);

  const contract = optionType === 'CE' ? strike.call : strike.put;
  const ltp = contract?.ltp ?? 100;
  const [limitPrice, setLimitPrice] = useState(ltp);

  const lotSize = indexSymbol.includes('SENSEX') ? 10 : 25;
  const totalQty = lots * lotSize;
  const estimatedValue = (orderType === 'MARKET' ? ltp : limitPrice) * totalQty;

  const isBuy = action === 'BUY';
  const colorClass = isBuy ? 'text-emerald-400' : 'text-rose-400';
  const btnClass = isBuy
    ? 'bg-emerald-600 hover:bg-emerald-500'
    : 'bg-rose-600 hover:bg-rose-500';

  const handleSubmit = () => {
    onConfirm({
      instrumentKey: contract?.instrumentKey || `${indexSymbol}_${strike.strikePrice}_${optionType}`,
      tradingSymbol: contract?.tradingSymbol || `${strike.strikePrice} ${optionType}`,
      indexSymbol,
      strikePrice: strike.strikePrice,
      optionType,
      transactionType: action,
      orderType,
      productType: 'I',
      lots,
      quantity: totalQty,
      price: orderType === 'MARKET' ? ltp : limitPrice,
      placedByMode: 'Manual',
    });
  };

  return (
    <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl relative">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-white"
        >
          <X className="w-5 h-5" />
        </button>

        <h3 className="text-lg font-bold text-white font-mono flex items-center space-x-2 mb-1">
          <span>MANUAL ORDER ENTRY</span>
        </h3>
        <p className="text-xs text-slate-400 font-mono mb-4">
          Execute order for <span className="text-white font-bold">{indexSymbol}</span>
        </p>

        {/* Selected Instrument Box */}
        <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 mb-4 flex items-center justify-between">
          <div>
            <div className={`text-base font-extrabold font-mono ${colorClass}`}>
              {action} {strike.strikePrice} {optionType}
            </div>
            <div className="text-[10px] text-slate-400 font-mono">
              Delta: {contract?.greeks?.delta?.toFixed(2) || '—'} | IV: {contract?.greeks?.iv?.toFixed(1) || '—'}%
            </div>
          </div>
          <div className="text-right font-mono">
            <div className="text-base font-extrabold text-white">₹{ltp.toFixed(2)}</div>
            <div className="text-[10px] text-slate-400">Current LTP</div>
          </div>
        </div>

        {/* Lots Selector */}
        <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 mb-4 flex items-center justify-between">
          <span className="text-xs text-slate-300 font-mono">Lots ({lotSize} Qty/Lot):</span>
          <div className="flex items-center space-x-1.5">
            {[1, 2, 5, 10].map((l) => (
              <button
                key={l}
                onClick={() => setLots(l)}
                className={`px-2.5 py-1 rounded text-xs font-mono font-bold transition ${
                  lots === l ? 'bg-cyan-600 text-white' : 'bg-slate-900 text-slate-400 hover:text-white'
                }`}
              >
                {l}
              </button>
            ))}
          </div>
        </div>

        {/* Order Type Toggle */}
        <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 mb-4 flex items-center justify-between">
          <span className="text-xs text-slate-300 font-mono">Order Type:</span>
          <div className="flex bg-slate-900 p-0.5 rounded-lg border border-slate-800">
            <button
              onClick={() => setOrderType('MARKET')}
              className={`px-3 py-1 rounded text-xs font-mono font-bold ${
                orderType === 'MARKET' ? 'bg-slate-800 text-white' : 'text-slate-400'
              }`}
            >
              MARKET
            </button>
            <button
              onClick={() => setOrderType('LIMIT')}
              className={`px-3 py-1 rounded text-xs font-mono font-bold ${
                orderType === 'LIMIT' ? 'bg-slate-800 text-white' : 'text-slate-400'
              }`}
            >
              LIMIT
            </button>
          </div>
        </div>

        {/* Limit Price Input if selected */}
        {orderType === 'LIMIT' && (
          <div className="bg-slate-950 p-3 rounded-xl border border-slate-800 mb-4 flex items-center justify-between">
            <span className="text-xs text-slate-300 font-mono">Limit Price:</span>
            <input
              type="number"
              step="0.05"
              value={limitPrice}
              onChange={(e) => setLimitPrice(parseFloat(e.target.value))}
              className="w-24 bg-slate-900 border border-slate-700 rounded px-2 py-1 text-right text-xs font-mono text-white focus:outline-none"
            />
          </div>
        )}

        {/* Value Estimation */}
        <div className="flex justify-between items-center text-xs font-mono text-slate-400 mb-4 px-1">
          <span>Total Executed Quantity:</span>
          <span className="text-white font-bold">{totalQty} Shares</span>
        </div>
        <div className="flex justify-between items-center text-xs font-mono text-slate-400 mb-5 px-1">
          <span>Estimated Premium Required:</span>
          <span className="text-cyan-400 font-bold">₹{estimatedValue.toLocaleString('en-IN', { maximumFractionDigits: 2 })}</span>
        </div>

        {/* Confirmation Lock */}
        <label className="flex items-center space-x-2 text-xs text-slate-300 cursor-pointer mb-5 p-2 rounded-lg bg-slate-950 border border-slate-800">
          <input
            type="checkbox"
            checked={confirmedSafety}
            onChange={(e) => setConfirmedSafety(e.target.checked)}
            className="w-4 h-4 rounded bg-slate-900 border-slate-700 text-cyan-600 focus:ring-cyan-500 cursor-pointer"
          />
          <span>Confirm live execution at broker exchange.</span>
        </label>

        {/* Buttons */}
        <div className="flex space-x-3">
          <button
            onClick={onClose}
            className="flex-1 py-2.5 bg-slate-800 hover:bg-slate-700 text-slate-300 font-mono text-xs font-bold rounded-xl transition"
          >
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            disabled={!confirmedSafety || isExecuting}
            className={`flex-1 py-2.5 text-white font-mono text-xs font-extrabold rounded-xl transition flex items-center justify-center space-x-2 shadow-lg ${
              confirmedSafety && !isExecuting
                ? `${btnClass} shadow-cyan-500/20`
                : 'bg-slate-800 text-slate-500 cursor-not-allowed'
            }`}
          >
            <ShieldCheck className="w-4 h-4" />
            <span>{isExecuting ? 'PLACING...' : `CONFIRM ${action}`}</span>
          </button>
        </div>
      </div>
    </div>
  );
};
