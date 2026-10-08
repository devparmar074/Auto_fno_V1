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
      <div className="bg-slate-900 border border-slate-700 rounded-2xl max-w-lg w-full p-6 shadow-2xl relative">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-white"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="flex items-center space-x-3 mb-4">
          <div className="w-10 h-10 rounded-xl bg-cyan-600/20 text-cyan-400 flex items-center justify-center font-bold">
            <KeyRound className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-white font-mono">Upstox API v2 Authentication</h2>
            <p className="text-xs text-slate-400 font-mono">Manage broker tokens & SEBI compliance</p>
          </div>
        </div>

        {/* Message notification */}
        {msg && (
          <div
            className={`p-3 rounded-xl mb-4 text-xs font-mono border ${
              msg.error
                ? 'bg-rose-950/40 border-rose-500/40 text-rose-300'
                : 'bg-emerald-950/40 border-emerald-500/40 text-emerald-300'
            }`}
          >
            {msg.text}
          </div>
        )}

        {/* Connection Status Box */}
        <div className="bg-slate-950 p-3.5 rounded-xl border border-slate-800 mb-5 font-mono">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs text-slate-400">Connection Status:</span>
            <span
              className={`text-xs font-bold px-2 py-0.5 rounded ${
                authStatus.isConnected
                  ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                  : 'bg-slate-800 text-slate-400'
              }`}
            >
              {authStatus.isConnected ? 'ACTIVE & CONNECTED' : 'DISCONNECTED'}
            </span>
          </div>

          {authStatus.isConnected && (
            <div className="space-y-1 text-xs border-t border-slate-800/80 pt-2 mt-2">
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
                <span className="text-cyan-400">
                  {authStatus.expiresAtUtc ? new Date(authStatus.expiresAtUtc).toLocaleTimeString() : '03:30 AM IST'}
                </span>
              </div>
            </div>
          )}
        </div>

        {/* Option 1: Official OAuth */}
        <div className="mb-5">
          <button
            onClick={handleOAuthLogin}
            disabled={loading}
            className="w-full py-2.5 bg-gradient-to-r from-cyan-600 to-emerald-600 hover:from-cyan-500 hover:to-emerald-500 text-white font-mono text-xs font-extrabold rounded-xl transition flex items-center justify-center space-x-2 shadow-lg shadow-cyan-600/20"
          >
            <ExternalLink className="w-4 h-4" />
            <span>CONNECT VIA UPSTOX OAUTH 2.0</span>
          </button>
        </div>

        <div className="relative flex py-2 items-center mb-4">
          <div className="flex-grow border-t border-slate-800"></div>
          <span className="flex-shrink mx-4 text-[10px] text-slate-500 uppercase font-mono">
            Or Paste Token Manually
          </span>
          <div className="flex-grow border-t border-slate-800"></div>
        </div>

        {/* Option 2: 1-Click Manual Token Paste */}
        <form onSubmit={handleManualTokenSubmit} className="mb-5">
          <div className="space-y-2">
            <textarea
              rows={2}
              placeholder="Paste raw Upstox Access Token here..."
              value={tokenInput}
              onChange={(e) => setTokenInput(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-xs font-mono text-white placeholder-slate-600 focus:outline-none focus:border-cyan-500 transition resize-none"
            />
            <button
              type="submit"
              disabled={loading || !tokenInput.trim()}
              className="w-full py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 font-mono text-xs font-bold rounded-xl transition disabled:opacity-50"
            >
              {loading ? 'Validating Token...' : 'Save & Activate Token'}
            </button>
          </div>
        </form>

        {/* SEBI Static IP Configuration */}
        <div className="border-t border-slate-800 pt-4">
          <div className="flex items-center space-x-2 text-xs font-bold text-slate-300 font-mono mb-2">
            <Globe className="w-3.5 h-3.5 text-cyan-400" />
            <span>SEBI Static IP Whitelist Manager</span>
          </div>
          <form onSubmit={handleUpdateIp} className="flex gap-2">
            <input
              type="text"
              placeholder="e.g. 103.21.58.12"
              value={primaryIp}
              onChange={(e) => setPrimaryIp(e.target.value)}
              className="flex-1 bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs font-mono text-white focus:outline-none focus:border-cyan-500"
            />
            <button
              type="submit"
              disabled={ipLoading || !primaryIp.trim()}
              className="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-xs font-mono text-slate-200 rounded-lg transition disabled:opacity-50"
            >
              {ipLoading ? 'Updating...' : 'Register IP'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
