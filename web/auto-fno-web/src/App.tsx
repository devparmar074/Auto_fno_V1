import React, { useCallback, useEffect, useRef, useState } from 'react';
import { fnoApi } from './api/fnoApi';
import { ErrorBoundary } from './components/ErrorBoundary';
import { HeaderBar } from './components/HeaderBar';
import { OptionChainMatrix } from './components/OptionChainMatrix';
import { OptionGreeksDeck } from './components/OptionGreeksDeck';
import { OrderConfirmationModal } from './components/OrderConfirmationModal';
import { PcrSentimentStrip } from './components/PcrSentimentStrip';
import { PositionsAndOrdersPanel } from './components/PositionsAndOrdersPanel';
import { SemiAutoAlertModal } from './components/SemiAutoAlertModal';
import { StrategyConvictionCard } from './components/StrategyConvictionCard';
import { TradingViewChart } from './components/TradingViewChart';
import { UpstoxConnectModal } from './components/UpstoxConnectModal';
import { useTradingHub } from './hooks/useTradingHub';
import type {
  BotConfig,
  BotMode,
  DashboardFnoSummaryDto,
  FnOStrategyScoreDto,
  IndexSymbol,
  MaxPainDto,
  OptionChainDto,
  OptionType,
  PcrSummaryDto,
  PlaceFnoOrderRequest,
  PositionDto,
  StrikeDataDto,
  UpstoxAuthStatusDto,
} from './types/fnoTrading';

export function App() {
  const [activeIndex, setActiveIndex] = useState<IndexSymbol>('NIFTY50');
  const [selectedExpiry, setSelectedExpiry] = useState<string>('');
  const [availableExpiries, setAvailableExpiries] = useState<string[]>([]);

  // Real-time market state
  const [spotPrice, setSpotPrice] = useState<number>(25150.25);
  const [prevSpotPrice, setPrevSpotPrice] = useState<number>(25100.00);
  const [spotChange, setSpotChange] = useState<number>(50.25);
  const [spotChangePercent, setSpotChangePercent] = useState<number>(0.20);
  const [isTickUp, setIsTickUp] = useState<boolean>(false);
  const [isTickDown, setIsTickDown] = useState<boolean>(false);
  const lastPriceRef = useRef<number>(spotPrice);

  // Derivatives and quantitative analytics
  const [optionChain, setOptionChain] = useState<OptionChainDto>({
    underlyingKey: 'NSE_INDEX|Nifty 50',
    underlyingSymbol: 'NIFTY50',
    spotPrice: 25150.25,
    prevSpotPrice: 25100.0,
    spotDayChange: 50.25,
    spotDayChangePercent: 0.20,
    expiryDate: '',
    availableExpiries: [],
    atmStrike: 25150,
    maxPainStrike: 25100,
    callWallStrike: 25300,
    putWallStrike: 25000,
    totalCallOi: 1250000,
    totalPutOi: 1450000,
    totalCallVolume: 850000,
    totalPutVolume: 920000,
    oiPcr: 1.16,
    volumePcr: 1.08,
    strikes: [],
    timestampUtc: new Date().toISOString(),
  });

  const [pcrSummary, setPcrSummary] = useState<PcrSummaryDto>({
    oiPcr: 1.16,
    volumePcr: 1.08,
    atmPcr: 1.12,
    prevOiPcr: 1.14,
    pcrSlope: 0.02,
    sentiment: 'Mildly Bullish',
    longBuildupCount: 6,
    shortBuildupCount: 2,
    shortCoveringCount: 4,
    longUnwindingCount: 1,
    totalCallOi: 1250000,
    totalPutOi: 1450000,
    callOiChangeTotal: 85000,
    putOiChangeTotal: 120000,
    calculatedAtUtc: new Date().toISOString(),
  });

  const [maxPain, setMaxPain] = useState<MaxPainDto>({
    maxPainStrike: 25100,
    totalPainValue: 450.5,
    spotPrice: 25150.25,
    distanceFromSpot: -50.25,
    distancePercent: -0.20,
    expiryDate: '',
    calculatedAtUtc: new Date().toISOString(),
  });

  const [strategyScore, setStrategyScore] = useState<FnOStrategyScoreDto>({
    indexSymbol: 'NIFTY50',
    spotPrice: 25150.25,
    totalScore: 68,
    pcrScore: 18,
    oiMaxPainScore: 16,
    greeksIvScore: 14,
    spotTrendScore: 20,
    recommendation: 'StrongBuyCall',
    setupRationale: 'Strong bullish alignment: Put writing dominating (PCR 1.16), Spot holding above EMA 9/21 with positive Delta bias.',
    recommendedStrike: '25150 CE',
    recommendedOptionType: 'CE',
    recommendedAction: 'BUY',
    recommendedEntryPrice: 112.50,
    recommendedStopLoss: 84.00,
    recommendedTarget: 162.00,
    expectedDelta: 0.52,
    expectedThetaDecay: -14.2,
    calculatedAtUtc: new Date().toISOString(),
  });

  const [positions, setPositions] = useState<PositionDto[]>([]);
  const [orders, setOrders] = useState<any[]>([]);
  const [totalUnrealizedPnl, setTotalUnrealizedPnl] = useState<number>(0);
  const [todayRealizedPnl, setTodayRealizedPnl] = useState<number>(0);

  const [botConfig, setBotConfig] = useState<BotConfig>({
    id: 1,
    mode: 'Manual',
    activeIndex: 'NIFTY50',
    defaultLots: 1,
    maxDailyLossLimit: 5000,
    maxDailyProfitTarget: 10000,
    stopLossPercent: 20,
    takeProfitPercent: 40,
    maxOpenPositions: 2,
    isKillSwitchActive: false,
    autoSquareOffAtCutoff: true,
    autoSquareOffTimeIst: '15:15:00',
  });

  const [authStatus, setAuthStatus] = useState<UpstoxAuthStatusDto>({
    isConnected: false,
    broker: 'UPSTOX',
  });

  const [marketStatus, setMarketStatus] = useState<string>('CLOSED');
  const [isRefreshing, setIsRefreshing] = useState<boolean>(false);
  const [isExecutingOrder, setIsExecutingOrder] = useState<boolean>(false);

  // Modals
  const [semiAutoAlert, setSemiAutoAlert] = useState<FnOStrategyScoreDto | null>(null);
  const [manualOrderModal, setManualOrderModal] = useState<{
    strike: StrikeDataDto;
    optionType: OptionType;
    action: 'BUY' | 'SELL';
  } | null>(null);
  const [isAuthModalOpen, setIsAuthModalOpen] = useState<boolean>(false);

  // Load Dashboard Data
  const loadDashboardData = useCallback(async (sym?: IndexSymbol) => {
    try {
      setIsRefreshing(true);
      const targetSym = sym || activeIndex;
      const data: DashboardFnoSummaryDto = await fnoApi.getDashboard(targetSym);

      setSpotPrice(data.spotPrice);
      setPrevSpotPrice(data.prevSpotPrice);
      setSpotChange(data.spotDayChange);
      setSpotChangePercent(data.spotDayChangePercent);
      setOptionChain(data.optionChain);
      setPcrSummary(data.pcrSummary);
      setMaxPain(data.maxPain);
      setStrategyScore(data.strategyScore);
      setPositions(data.openPositions || []);
      setTotalUnrealizedPnl(data.totalUnrealizedPnl);
      setTodayRealizedPnl(data.todayRealizedPnl);
      setBotConfig(data.botConfig);
      setMarketStatus(data.marketStatus);

      if (data.optionChain?.availableExpiries?.length) {
        setAvailableExpiries(data.optionChain.availableExpiries);
        if (!selectedExpiry) {
          setSelectedExpiry(data.optionChain.expiryDate || data.optionChain.availableExpiries[0]);
        }
      }

      // Also fetch orders & auth status
      const ords = await fnoApi.getOrders();
      setOrders(ords);

      const auth = await fnoApi.getAuthStatus();
      setAuthStatus(auth);
    } catch (err) {
      console.error('Failed loading dashboard:', err);
    } finally {
      setIsRefreshing(false);
    }
  }, [activeIndex, selectedExpiry]);

  useEffect(() => {
    loadDashboardData();
  }, [activeIndex]);

  // Real-Time SignalR WebSocket Hook
  const { isConnected } = useTradingHub({
    onSpotQuote: (quote) => {
      if (quote.indexSymbol.toUpperCase() === activeIndex.toUpperCase()) {
        const newPrice = quote.ltp;
        if (newPrice > lastPriceRef.current) {
          setIsTickUp(true);
          setIsTickDown(false);
        } else if (newPrice < lastPriceRef.current) {
          setIsTickDown(true);
          setIsTickUp(false);
        }
        lastPriceRef.current = newPrice;
        setSpotPrice(newPrice);
        setSpotChange(quote.dayChange);
        setSpotChangePercent(quote.dayChangePercent);

        setTimeout(() => {
          setIsTickUp(false);
          setIsTickDown(false);
        }, 700);
      }
    },
    onOptionChain: (chain) => {
      if (chain.underlyingSymbol.toUpperCase() === activeIndex.toUpperCase()) {
        setOptionChain(chain);
      }
    },
    onPcrMetrics: (metrics) => {
      setPcrSummary(metrics.pcr);
      setMaxPain(metrics.maxPain);
    },
    onStrategyScore: (score) => {
      if (score.indexSymbol.toUpperCase() === activeIndex.toUpperCase()) {
        setStrategyScore(score);
      }
    },
    onPositions: (data) => {
      setPositions(data.positions || []);
      setTotalUnrealizedPnl(data.totalUnrealizedPnl);
      setTodayRealizedPnl(data.todayRealizedPnl);
    },
    onSemiAutoAlert: (alert) => {
      if (botConfig.mode === 'SemiAuto') {
        setSemiAutoAlert(alert);
      }
    },
    onOrderUpdate: () => {
      loadDashboardData();
    },
  });

  // Switch Index
  const handleSelectIndex = async (index: IndexSymbol) => {
    setActiveIndex(index);
    await fnoApi.switchIndex(index);
    loadDashboardData(index);
  };

  // Switch Expiry
  const handleSelectExpiry = async (exp: string) => {
    setSelectedExpiry(exp);
    await fnoApi.selectExpiry(exp);
    const chain = await fnoApi.getOptionChain(activeIndex, exp);
    setOptionChain(chain);
  };

  // Switch Bot Mode
  const handleSelectBotMode = async (mode: BotMode) => {
    const updated = await fnoApi.setBotMode(mode);
    setBotConfig(updated);
  };

  // Trigger Kill Switch
  const handleTriggerKillSwitch = async () => {
    await fnoApi.triggerKillSwitch();
    setBotConfig((prev) => ({ ...prev, isKillSwitchActive: true, mode: 'Manual' }));
    loadDashboardData();
  };

  // Quick Order from Option Chain
  const handleQuickOrder = (strike: StrikeDataDto, optionType: OptionType, action: 'BUY' | 'SELL') => {
    setManualOrderModal({ strike, optionType, action });
  };

  // Confirm Manual Order
  const handleConfirmManualOrder = async (req: PlaceFnoOrderRequest) => {
    try {
      setIsExecutingOrder(true);
      await fnoApi.placeOrder(req);
      setManualOrderModal(null);
      await loadDashboardData();
    } catch (err: any) {
      alert(`Order Failed: ${err.message || 'Error executing order'}`);
    } finally {
      setIsExecutingOrder(false);
    }
  };

  // Confirm Semi-Auto Order
  const handleConfirmSemiAuto = async (lots: number) => {
    if (!semiAutoAlert) return;
    try {
      setIsExecutingOrder(true);
      await fnoApi.placeOrder({
        instrumentKey: `${activeIndex}_${semiAutoAlert.recommendedStrike}`,
        tradingSymbol: `${activeIndex}_${semiAutoAlert.recommendedStrike}`,
        indexSymbol: activeIndex,
        optionType: semiAutoAlert.recommendedOptionType,
        transactionType: semiAutoAlert.recommendedAction,
        orderType: 'MARKET',
        productType: 'I',
        lots,
        quantity: lots * (activeIndex === 'SENSEX' ? 10 : 25),
        price: semiAutoAlert.recommendedEntryPrice,
        stopLossPrice: semiAutoAlert.recommendedStopLoss,
        takeProfitPrice: semiAutoAlert.recommendedTarget,
        placedByMode: 'SemiAuto',
      });
      setSemiAutoAlert(null);
      await loadDashboardData();
    } catch (err: any) {
      alert(`Semi-Auto Execution Failed: ${err.message}`);
    } finally {
      setIsExecutingOrder(false);
    }
  };

  // Square off single position
  const handleSquareOffPosition = async (instrumentKey: string) => {
    try {
      setIsExecutingOrder(true);
      await fnoApi.squareOffPosition(instrumentKey);
      await loadDashboardData();
    } catch (err: any) {
      alert(`Exit failed: ${err.message}`);
    } finally {
      setIsExecutingOrder(false);
    }
  };

  // Square off all positions
  const handleSquareOffAll = async () => {
    try {
      setIsExecutingOrder(true);
      await fnoApi.squareOffAll();
      await loadDashboardData();
    } catch (err: any) {
      alert(`Exit all failed: ${err.message}`);
    } finally {
      setIsExecutingOrder(false);
    }
  };

  return (
    <ErrorBoundary>
      <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans selection:bg-cyan-500 selection:text-slate-950">
        {/* Navigation & Control Header */}
        <HeaderBar
          activeIndex={activeIndex}
          onSelectIndex={handleSelectIndex}
          spotPrice={spotPrice}
          prevSpotPrice={prevSpotPrice}
          spotChange={spotChange}
          spotChangePercent={spotChangePercent}
          isTickUp={isTickUp}
          isTickDown={isTickDown}
          availableExpiries={availableExpiries}
          selectedExpiry={selectedExpiry}
          onSelectExpiry={handleSelectExpiry}
          marketStatus={marketStatus}
          botMode={botConfig.mode}
          onSelectBotMode={handleSelectBotMode}
          isKillSwitchActive={botConfig.isKillSwitchActive}
          onTriggerKillSwitch={handleTriggerKillSwitch}
          authStatus={authStatus}
          onOpenAuthModal={() => setIsAuthModalOpen(true)}
          onRefresh={() => loadDashboardData()}
          isRefreshing={isRefreshing}
        />

        {/* Main Workstation Cockpit */}
        <main className="flex-1 max-w-[1720px] w-full mx-auto px-4 py-2">
          {/* Top Analytics Strip: PCR, Max Pain, Walls, Buildup */}
          <PcrSentimentStrip
            pcr={pcrSummary}
            maxPain={maxPain}
            callWall={optionChain.callWallStrike}
            putWall={optionChain.putWallStrike}
            spotPrice={spotPrice}
          />

          {/* Quantitative Strategy Conviction Card (-100 to +100) */}
          <StrategyConvictionCard
            score={strategyScore}
            onExecuteRecommendation={() => {
              if (strategyScore.recommendedStrike) {
                setSemiAutoAlert(strategyScore);
              }
            }}
          />

          {/* Real-Time Option Chain Matrix */}
          <OptionChainMatrix
            chain={optionChain}
            onQuickOrder={handleQuickOrder}
          />

          {/* Greeks Matrix Deck */}
          <OptionGreeksDeck chain={optionChain} />

          {/* Intraday Candlestick Chart */}
          <TradingViewChart
            indexSymbol={activeIndex}
            spotPrice={spotPrice}
          />

          {/* Positions & Order Ledger */}
          <PositionsAndOrdersPanel
            positions={positions}
            orders={orders}
            totalUnrealizedPnl={totalUnrealizedPnl}
            todayRealizedPnl={todayRealizedPnl}
            onSquareOff={handleSquareOffPosition}
            onSquareOffAll={handleSquareOffAll}
            isSquaringOff={isExecutingOrder}
          />
        </main>

        {/* Modals */}
        {semiAutoAlert && (
          <SemiAutoAlertModal
            alert={semiAutoAlert}
            onConfirm={handleConfirmSemiAuto}
            onDismiss={() => setSemiAutoAlert(null)}
            isExecuting={isExecutingOrder}
          />
        )}

        {manualOrderModal && (
          <OrderConfirmationModal
            strike={manualOrderModal.strike}
            optionType={manualOrderModal.optionType}
            action={manualOrderModal.action}
            indexSymbol={activeIndex}
            onConfirm={handleConfirmManualOrder}
            onClose={() => setManualOrderModal(null)}
            isExecuting={isExecutingOrder}
          />
        )}

        {isAuthModalOpen && (
          <UpstoxConnectModal
            authStatus={authStatus}
            onClose={() => setIsAuthModalOpen(false)}
            onRefreshAuth={() => fnoApi.getAuthStatus().then(setAuthStatus)}
          />
        )}
      </div>
    </ErrorBoundary>
  );
}

export default App;
