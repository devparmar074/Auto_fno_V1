export type BotMode = 'Manual' | 'SemiAuto' | 'FullAuto';
export type OptionType = 'CE' | 'PE';
export type IndexSymbol = 'NIFTY50' | 'SENSEX';
export type BuildupType = 'Neutral' | 'LongBuildup' | 'ShortBuildup' | 'ShortCovering' | 'LongUnwinding';
export type StrategyRecommendation = 
  | 'Neutral' 
  | 'StrongBuyCall' 
  | 'StrongBuyPut' 
  | 'StrongSellCall' 
  | 'StrongSellPut' 
  | 'RangeboundHold' 
  | 'ExitAll';

export interface OptionGreeksDto {
  delta: number;
  gamma: number;
  theta: number;
  vega: number;
  rho: number;
  iv: number;
  pop: number;
}

export interface OptionContractDetailDto {
  instrumentKey: string;
  tradingSymbol: string;
  ltp: number;
  prevLtp: number;
  closePrice?: number;
  change: number;
  changePercent: number;
  volume: number;
  openInterest: number;
  prevOpenInterest: number;
  oiChange: number;
  oiChangePercent: number;
  bidPrice: number;
  bidQty: number;
  askPrice: number;
  askQty: number;
  greeks: OptionGreeksDto;
  buildup: BuildupType;
}

export interface StrikeDataDto {
  strikePrice: number;
  isAtm: boolean;
  isMaxPain: boolean;
  isCallWall: boolean;
  isPutWall: boolean;
  call?: OptionContractDetailDto;
  put?: OptionContractDetailDto;
  strikePcr: number;
}

export interface OptionChainDto {
  underlyingKey: string;
  underlyingSymbol: string;
  spotPrice: number;
  prevSpotPrice: number;
  spotDayChange: number;
  spotDayChangePercent: number;
  expiryDate: string;
  availableExpiries: string[];
  atmStrike: number;
  maxPainStrike: number;
  callWallStrike: number;
  putWallStrike: number;
  totalCallOi: number;
  totalPutOi: number;
  totalCallVolume: number;
  totalPutVolume: number;
  oiPcr: number;
  volumePcr: number;
  strikes: StrikeDataDto[];
  timestampUtc: string;
}

export interface PcrSummaryDto {
  oiPcr: number;
  volumePcr: number;
  atmPcr: number;
  prevOiPcr: number;
  pcrSlope: number;
  sentiment: string;
  longBuildupCount: number;
  shortBuildupCount: number;
  shortCoveringCount: number;
  longUnwindingCount: number;
  totalCallOi: number;
  totalPutOi: number;
  callOiChangeTotal: number;
  putOiChangeTotal: number;
  calculatedAtUtc: string;
}

export interface MaxPainDto {
  maxPainStrike: number;
  totalPainValue: number;
  spotPrice: number;
  distanceFromSpot: number;
  distancePercent: number;
  expiryDate: string;
  calculatedAtUtc: string;
}

export interface FnOStrategyScoreDto {
  indexSymbol: string;
  spotPrice: number;
  totalScore: number; // -100 to +100
  pcrScore: number;
  oiMaxPainScore: number;
  greeksIvScore: number;
  spotTrendScore: number;
  recommendation: StrategyRecommendation;
  setupRationale: string;
  recommendedStrike: string;
  recommendedOptionType?: OptionType;
  recommendedAction: 'BUY' | 'SELL';
  recommendedEntryPrice: number;
  recommendedStopLoss: number;
  recommendedTarget: number;
  expectedDelta: number;
  expectedThetaDecay: number;
  calculatedAtUtc: string;
}

export interface PlaceFnoOrderRequest {
  instrumentKey: string;
  tradingSymbol: string;
  indexSymbol: string;
  strikePrice?: number;
  optionType?: OptionType;
  transactionType: 'BUY' | 'SELL';
  orderType: 'MARKET' | 'LIMIT' | 'SL';
  productType: 'I' | 'D';
  lots: number;
  quantity: number;
  price: number;
  triggerPrice?: number;
  stopLossPrice?: number;
  takeProfitPrice?: number;
  placedByMode: BotMode;
  correlationId?: string;
}

export interface OrderResultDto {
  success: boolean;
  orderId?: string;
  correlationId?: string;
  message: string;
  executedPrice: number;
  status: string;
  timestampUtc: string;
}

export interface PositionDto {
  instrumentKey: string;
  tradingSymbol: string;
  indexSymbol: string;
  strikePrice?: number;
  optionType?: OptionType;
  quantity: number;
  lots: number;
  averagePrice: number;
  currentLtp: number;
  unrealizedPnl: number;
  realizedPnl: number;
  totalPnl: number;
  pnlPercent: number;
}

export interface BotConfig {
  id: number;
  mode: BotMode;
  activeIndex: string;
  defaultLots: number;
  maxDailyLossLimit: number;
  maxDailyProfitTarget: number;
  stopLossPercent: number;
  takeProfitPercent: number;
  maxOpenPositions: number;
  isKillSwitchActive: boolean;
  autoSquareOffAtCutoff: boolean;
  autoSquareOffTimeIst: string;
}

export interface DashboardFnoSummaryDto {
  indexSymbol: string;
  spotPrice: number;
  prevSpotPrice: number;
  spotDayChange: number;
  spotDayChangePercent: number;
  optionChain: OptionChainDto;
  pcrSummary: PcrSummaryDto;
  maxPain: MaxPainDto;
  strategyScore: FnOStrategyScoreDto;
  openPositions: PositionDto[];
  totalUnrealizedPnl: number;
  todayRealizedPnl: number;
  botConfig: BotConfig;
  marketStatus: string;
  isTokenValid: boolean;
  serverTimeUtc: string;
}

export interface UpstoxAuthStatusDto {
  isConnected: boolean;
  userId?: string;
  userName?: string;
  email?: string;
  primaryIp?: string;
  expiresAtUtc?: string;
  broker: string;
}

export interface Candle {
  timestampUtc: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  openInterest: number;
}
