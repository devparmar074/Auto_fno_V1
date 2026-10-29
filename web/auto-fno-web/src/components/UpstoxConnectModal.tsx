import { CheckCircle2, Copy, ExternalLink, Globe, KeyRound, ShieldCheck, UserCheck, X } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import { fnoApi } from '../api/fnoApi';
import type { UpstoxAuthStatusDto } from '../types/fnoTrading';

interface UpstoxConnectModalProps {
  authStatus: UpstoxAuthStatusDto;
  onClose: () => void;
  onRefreshAuth: () => void;
}

export const UpstoxConnectModal: React.FC<UpstoxConnectModalProps> = ({
  authStatus,
  onClose,
  onRefreshAuth,
}) => {
  const [tokenInput, setTokenInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<{ text: string; error: boolean } | null>(null);

  // SEBI Static IP
  const [primaryIp, setPrimaryIp] = useState('');
  const [secondaryIp, setSecondaryIp] = useState('');
  const [ipLoading, setIpLoading] = useState(false);

  useEffect(() => {
    async function loadIp() {
      try {
        const ip = await fnoApi.getRegisteredIp();
        if (ip) setPrimaryIp(ip);
      } catch {
        // ignore
      }
    }
    if (authStatus.isConnected) {
      loadIp();
    }
  }, [authStatus.isConnected]);

  const handleOAuthLogin = async () => {
    try {
      setLoading(true);
      const { authorizationUrl } = await fnoApi.getLoginUrl();
      window.location.href = authorizationUrl;
    } catch (err: any) {
      setMsg({ text: err.message || 'Failed opening Upstox auth URL', error: true });
      setLoading(false);
    }
  };

  const handleManualTokenSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!tokenInput.trim()) return;

    try {
      setLoading(true);
      setMsg(null);
      await fnoApi.setManualToken(tokenInput.trim());
      setMsg({ text: 'Access Token validated and saved successfully!', error: false });
      setTokenInput('');
      onRefreshAuth();
    } catch (err: any) {
      setMsg({ text: err.message || 'Invalid or expired token', error: true });
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateIp = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setIpLoading(true);
      await fnoApi.setRegisteredIp(primaryIp, secondaryIp);
      setMsg({ text: 'Static IP successfully registered with Upstox API.', error: false });
    } catch (err: any) {
      setMsg({ text: err.message || 'Failed updating static IP', error: true });
    } finally {
      setIpLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/85 backdrop-blur-md z-50 flex items-center justify-center p-4">
      <div className="ob-panel rounded-2xl max-w-lg w-full p-6 shadow-2xl relative border border-[#1A212D] overflow-hidden">
        {/* Scanner Radar Beam across Modal */}
        <div className="ob-scanner-beam" />

        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-white transition ob-hover-lift z-20"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="flex items-center space-x-3 mb-4 relative z-10">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-[#2DD4BF] to-[#45B8FF] p-[1px] shadow-lg shadow-cyan-500/20">
            <div className="w-full h-full bg-[#06080C] rounded-xl flex items-center justify-center">
              <KeyRound className="w-5 h-5 text-[#45B8FF] animate-pulse" />
            </div>
          </div>
          <div>
            <h2 className="text-lg font-bold text-white font-mono ob-grad-brand">Upstox API v2 Authentication</h2>
            <p className="text-xs text-slate-400 font-mono">Manage broker tokens & SEBI compliance</p>
          </div>
        </div>

        {/* Message notification */}
        {msg && (
          <div
            className={`p-3 rounded-xl mb-4 text-xs font-mono border relative z-10 ${
              msg.error
                ? 'bg-[#FF4D67]/15 border-[#FF4D67]/40 text-[#FF4D67]'
                : 'bg-[#19E58C]/15 border-[#19E58C]/40 text-[#19E58C]'
            }`}
          >
            {msg.text}
          </div>
        )}

        {/* Connection Status Box */}
        <div className="bg-[#06080C] p-3.5 rounded-xl border border-[#1A212D] mb-5 font-mono relative z-10 ob-hover-lift shadow-inner">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs text-slate-400">Connection Status:</span>
            <div className="flex items-center space-x-2">
              {authStatus.isConnected && <span className="ob-sonar-dot" />}
              <span
                className={`text-xs font-bold px-2 py-0.5 rounded ${
                  authStatus.isConnected
                    ? 'bg-[#19E58C]/10 text-[#19E58C] border border-[#19E58C]/40'
                    : 'bg-[#11161F] text-slate-400 border border-[#1A212D]'
                }`}
              >
                {authStatus.isConnected ? 'ACTIVE & CONNECTED' : 'DISCONNECTED'}
              </span>
            </div>
          </div>

          {authStatus.isConnected && (
            <div className="space-y-1.5 text-xs border-t border-[#1A212D] pt-2.5 mt-2">
              <div className="flex justify-between">
                <span className="text-slate-400">User ID:</span>
                <span className="text-white font-bold">{authStatus.userId}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-400">User Name:</span>
                <span className="text-white">{authStatus.userName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-400">Token Expiry:</span>
                <span className="text-[#45B8FF] font-bold">
                  {authStatus.expiresAtUtc ? new Date(authStatus.expiresAtUtc).toLocaleTimeString() : '03:30 AM IST'}
                </span>
              </div>
            </div>
          )}
        </div>

        {/* Option 1: Official OAuth */}
        <div className="mb-5 relative z-10">
          <button
            onClick={handleOAuthLogin}
            disabled={loading}
            className="w-full py-3 ob-btn-bull text-white font-mono text-xs font-extrabold rounded-xl transition flex items-center justify-center space-x-2 shadow-xl"
          >
            <ExternalLink className="w-4 h-4 animate-bounce" />
            <span>CONNECT VIA UPSTOX OAUTH 2.0</span>
          </button>
        </div>

        <div className="relative flex py-2 items-center mb-4 z-10">
          <div className="flex-grow border-t border-[#1A212D]"></div>
          <span className="flex-shrink mx-4 text-[10px] text-slate-500 uppercase font-mono">
            Or Paste Token Manually
          </span>
          <div className="flex-grow border-t border-[#1A212D]"></div>
        </div>

        {/* Option 2: 1-Click Manual Token Paste */}
        <form onSubmit={handleManualTokenSubmit} className="mb-5 relative z-10">
          <div className="space-y-2">
            <textarea
              rows={2}
              placeholder="Paste raw Upstox Access Token here..."
              value={tokenInput}
              onChange={(e) => setTokenInput(e.target.value)}
              className="w-full bg-[#06080C] border border-[#1A212D] rounded-xl p-3 text-xs font-mono text-white placeholder-slate-600 focus:outline-none focus:border-cyan-500 transition resize-none shadow-inner"
            />
            <button
              type="submit"
              disabled={loading || !tokenInput.trim()}
              className="w-full py-2.5 bg-[#1A212D] hover:bg-[#242D3C] text-slate-200 font-mono text-xs font-bold rounded-xl transition disabled:opacity-50 ob-hover-lift border border-[#242D3C]"
            >
              {loading ? 'Validating Token...' : 'Save & Activate Token'}
            </button>
          </div>
        </form>

        {/* SEBI Static IP Configuration */}
        <div className="border-t border-[#1A212D] pt-4 relative z-10">
          <div className="flex items-center space-x-2 text-xs font-bold text-slate-300 font-mono mb-2">
            <Globe className="w-3.5 h-3.5 text-[#45B8FF]" />
            <span>SEBI Static IP Whitelist Manager</span>
          </div>
          <form onSubmit={handleUpdateIp} className="flex gap-2">
            <input
              type="text"
              placeholder="e.g. 103.21.58.12"
              value={primaryIp}
              onChange={(e) => setPrimaryIp(e.target.value)}
              className="flex-1 bg-[#06080C] border border-[#1A212D] rounded-lg px-3 py-1.5 text-xs font-mono text-white focus:outline-none focus:border-cyan-500 shadow-inner"
            />
            <button
              type="submit"
              disabled={ipLoading || !primaryIp.trim()}
              className="px-3.5 py-1.5 bg-[#1A212D] hover:bg-[#242D3C] text-xs font-mono text-slate-200 rounded-lg transition disabled:opacity-50 ob-hover-lift border border-[#242D3C]"
            >
              {ipLoading ? 'Updating...' : 'Register IP'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
