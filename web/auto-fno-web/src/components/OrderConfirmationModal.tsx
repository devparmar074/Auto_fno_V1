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
    <div className="fixed inset-0 bg-black/85 backdrop-blur-md z-50 flex items-center justify-center p-4">
      <div
        className={`ob-panel rounded-2xl max-w-md w-full p-6 shadow-2xl relative border overflow-hidden transition-all duration-300 ${
          isBuy ? 'border-[#19E58C]/40 ob-glow-bull' : 'border-[#FF4D67]/40 ob-glow-bear'
        }`}
      >
        {/* Scanner Radar Beam across Modal */}
        <div className="ob-scanner-beam" />

        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-white transition ob-hover-lift z-20"
        >
          <X className="w-5 h-5" />
        </button>

        <h3 className="text-lg font-bold text-white font-mono flex items-center space-x-2 mb-1 relative z-10">
          <span className="ob-grad-brand">MANUAL ORDER ENTRY</span>
        </h3>
        <p className="text-xs text-slate-400 font-mono mb-4 relative z-10">
          Execute order for <span className="text-white font-bold">{indexSymbol}</span>
        </p>

        {/* Selected Instrument Box */}
        <div className="bg-[#06080C] p-3.5 rounded-xl border border-[#1A212D] mb-4 flex items-center justify-between relative z-10 ob-hover-lift">
          <div>
            <div className={`text-base font-extrabold font-mono ${isBuy ? 'text-[#19E58C]' : 'text-[#FF4D67]'}`}>
              {action} {strike.strikePrice} {optionType}
            </div>
            <div className="text-[10px] text-slate-400 font-mono mt-0.5">
              Delta: {contract?.greeks?.delta?.toFixed(2) || '—'} | IV: {contract?.greeks?.iv?.toFixed(1) || '—'}%
            </div>
          </div>
          <div className="text-right font-mono">
            <div className="text-base font-extrabold text-white">₹{ltp.toFixed(2)}</div>
            <div className="text-[10px] text-slate-400">Current LTP</div>
          </div>
        </div>

        {/* Lots Selector */}
        <div className="bg-[#06080C] p-3 rounded-xl border border-[#1A212D] mb-4 flex items-center justify-between relative z-10">
          <span className="text-xs text-slate-300 font-mono">Lots ({lotSize} Qty/Lot):</span>
          <div className="flex items-center space-x-1.5">
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
                {l}
              </button>
            ))}
          </div>
        </div>

        {/* Order Type Toggle */}
        <div className="bg-[#06080C] p-3 rounded-xl border border-[#1A212D] mb-4 flex items-center justify-between relative z-10">
          <span className="text-xs text-slate-300 font-mono">Order Type:</span>
          <div className="flex bg-[#11161F] p-0.5 rounded-lg border border-[#1A212D]">
            <button
              onClick={() => setOrderType('MARKET')}
              className={`px-3 py-1 rounded text-xs font-mono font-bold transition ${
                orderType === 'MARKET' ? 'bg-[#1A212D] text-white shadow-sm' : 'text-slate-400'
              }`}
            >
              MARKET
            </button>
            <button
              onClick={() => setOrderType('LIMIT')}
              className={`px-3 py-1 rounded text-xs font-mono font-bold transition ${
                orderType === 'LIMIT' ? 'bg-[#1A212D] text-white shadow-sm' : 'text-slate-400'
              }`}
            >
              LIMIT
            </button>
          </div>
        </div>

        {/* Limit Price Input if selected */}
        {orderType === 'LIMIT' && (
          <div className="bg-[#06080C] p-3 rounded-xl border border-[#1A212D] mb-4 flex items-center justify-between relative z-10">
            <span className="text-xs text-slate-300 font-mono">Limit Price:</span>
            <input
              type="number"
              step="0.05"
              value={limitPrice}
              onChange={(e) => setLimitPrice(parseFloat(e.target.value))}
              className="w-24 bg-[#11161F] border border-[#1A212D] rounded px-2 py-1 text-right text-xs font-mono text-white focus:outline-none focus:border-cyan-500"
            />
          </div>
        )}

        {/* Value Estimation */}
        <div className="flex justify-between items-center text-xs font-mono text-slate-400 mb-2 px-1 relative z-10">
          <span>Total Executed Quantity:</span>
          <span className="text-white font-bold">{totalQty} Shares</span>
        </div>
        <div className="flex justify-between items-center text-xs font-mono text-slate-400 mb-4 px-1 relative z-10">
          <span>Estimated Premium Required:</span>
          <span className="text-[#45B8FF] font-bold">₹{estimatedValue.toLocaleString('en-IN', { maximumFractionDigits: 2 })}</span>
        </div>

        {/* Confirmation Lock */}
        <label className="flex items-center space-x-2.5 text-xs text-slate-300 cursor-pointer mb-5 p-2.5 rounded-xl bg-[#06080C] border border-[#1A212D] relative z-10">
          <input
            type="checkbox"
            checked={confirmedSafety}
            onChange={(e) => setConfirmedSafety(e.target.checked)}
            className="w-4 h-4 rounded bg-[#11161F] border-[#1A212D] text-[#45B8FF] focus:ring-[#45B8FF] cursor-pointer"
          />
          <span className="font-mono text-[11px]">Confirm live execution at broker exchange.</span>
        </label>

        {/* Buttons */}
        <div className="flex space-x-3 relative z-10">
          <button
            onClick={onClose}
            className="flex-1 py-2.5 bg-[#1A212D] hover:bg-[#242D3C] text-slate-300 font-mono text-xs font-bold rounded-xl transition ob-hover-lift"
          >
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            disabled={!confirmedSafety || isExecuting}
            className={`flex-1 py-2.5 text-white font-mono text-xs font-extrabold rounded-xl transition flex items-center justify-center space-x-2 shadow-xl ${
              confirmedSafety && !isExecuting
                ? isBuy ? 'ob-btn-bull' : 'ob-btn-bear'
                : 'bg-[#11161F] text-slate-600 cursor-not-allowed border border-[#1A212D]'
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
