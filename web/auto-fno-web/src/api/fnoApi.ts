import type {
  BotConfig,
  BotMode,
  Candle,
  DashboardFnoSummaryDto,
  FnOStrategyScoreDto,
  MaxPainDto,
  OptionChainDto,
  OrderResultDto,
  PcrSummaryDto,
  PlaceFnoOrderRequest,
  PositionDto,
  UpstoxAuthStatusDto,
} from '../types/fnoTrading';

const API_BASE = '/api';

export const fnoApi = {
  // Dashboard & Summary
  async getDashboard(symbol?: string): Promise<DashboardFnoSummaryDto> {
    const res = await fetch(`${API_BASE}/marketdata/dashboard${symbol ? `?symbol=${symbol}` : ''}`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  // Option Chain & Derivatives Analytics
  async getOptionChain(symbol?: string, expiry?: string): Promise<OptionChainDto> {
    const params = new URLSearchParams();
    if (symbol) params.append('symbol', symbol);
    if (expiry) params.append('expiry', expiry);
    const res = await fetch(`${API_BASE}/optionchain/chain?${params.toString()}`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getExpiries(symbol?: string): Promise<string[]> {
    const res = await fetch(`${API_BASE}/optionchain/expiries${symbol ? `?symbol=${symbol}` : ''}`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getPcr(symbol?: string): Promise<PcrSummaryDto> {
    const res = await fetch(`${API_BASE}/optionchain/pcr${symbol ? `?symbol=${symbol}` : ''}`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getMaxPain(symbol?: string): Promise<MaxPainDto> {
    const res = await fetch(`${API_BASE}/optionchain/maxpain${symbol ? `?symbol=${symbol}` : ''}`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async switchIndex(indexSymbol: string): Promise<void> {
    await fetch(`${API_BASE}/optionchain/switch-index`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ indexSymbol }),
    });
  },

  async selectExpiry(expiryDate: string): Promise<void> {
    await fetch(`${API_BASE}/optionchain/select-expiry`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ expiryDate }),
    });
  },

  // Strategy & Bot Configuration
  async getStrategyScore(symbol?: string): Promise<FnOStrategyScoreDto> {
    const res = await fetch(`${API_BASE}/fnostrategy/score${symbol ? `?symbol=${symbol}` : ''}`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getBotConfig(): Promise<BotConfig> {
    const res = await fetch(`${API_BASE}/fnostrategy/config`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async updateBotConfig(config: BotConfig): Promise<BotConfig> {
    const res = await fetch(`${API_BASE}/fnostrategy/config`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(config),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async setBotMode(mode: BotMode): Promise<BotConfig> {
    const res = await fetch(`${API_BASE}/fnostrategy/mode`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ mode }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async triggerKillSwitch(): Promise<{ success: boolean; message: string }> {
    const res = await fetch(`${API_BASE}/fnostrategy/kill-switch`, { method: 'POST' });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  // Orders & Positions
  async placeOrder(req: PlaceFnoOrderRequest): Promise<OrderResultDto> {
    const res = await fetch(`${API_BASE}/fnotrade/order`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(req),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async squareOffPosition(instrumentKey: string): Promise<boolean> {
    const res = await fetch(`${API_BASE}/fnotrade/square-off`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ instrumentKey }),
    });
    if (!res.ok) throw new Error(await res.text());
    const data = await res.json();
    return data.success;
  },

  async squareOffAll(): Promise<number> {
    const res = await fetch(`${API_BASE}/fnotrade/square-off-all`, { method: 'POST' });
    if (!res.ok) throw new Error(await res.text());
    const data = await res.json();
    return data.closedCount;
  },

  async getPositions(): Promise<PositionDto[]> {
    const res = await fetch(`${API_BASE}/fnotrade/positions`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getOrders(): Promise<any[]> {
    const res = await fetch(`${API_BASE}/fnotrade/orders`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getCandles(symbol?: string, interval = '1minute'): Promise<Candle[]> {
    const params = new URLSearchParams();
    if (symbol) params.append('symbol', symbol);
    params.append('interval', interval);
    const res = await fetch(`${API_BASE}/marketdata/candles?${params.toString()}`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  // Upstox Auth & SEBI IP
  async getAuthStatus(): Promise<UpstoxAuthStatusDto> {
    const res = await fetch(`${API_BASE}/upstoxauth/status`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getLoginUrl(): Promise<{ authorizationUrl: string }> {
    const res = await fetch(`${API_BASE}/upstoxauth/login`);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async setManualToken(accessToken: string, refreshToken?: string): Promise<UpstoxAuthStatusDto> {
    const res = await fetch(`${API_BASE}/upstoxauth/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ accessToken, refreshToken }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async getRegisteredIp(): Promise<string | null> {
    const res = await fetch(`${API_BASE}/upstoxauth/ip`);
    if (!res.ok) return null;
    const data = await res.json();
    return data.registeredIp;
  },

  async setRegisteredIp(primaryIp: string, secondaryIp?: string): Promise<any> {
    const res = await fetch(`${API_BASE}/upstoxauth/ip`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ primaryIp, secondaryIp }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }
};
