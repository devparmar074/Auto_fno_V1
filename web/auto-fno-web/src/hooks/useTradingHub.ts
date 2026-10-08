import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';
import type {
  FnOStrategyScoreDto,
  MaxPainDto,
  OptionChainDto,
  OrderResultDto,
  PcrSummaryDto,
  PositionDto,
} from '../types/fnoTrading';

interface UseTradingHubProps {
  onSpotQuote?: (quote: { indexSymbol: string; ltp: number; dayChange: number; dayChangePercent: number }) => void;
  onOptionChain?: (chain: OptionChainDto) => void;
  onPcrMetrics?: (metrics: { pcr: PcrSummaryDto; maxPain: MaxPainDto }) => void;
  onStrategyScore?: (score: FnOStrategyScoreDto) => void;
  onPositions?: (data: { positions: PositionDto[]; totalUnrealizedPnl: number; todayRealizedPnl: number }) => void;
  onSemiAutoAlert?: (alert: FnOStrategyScoreDto) => void;
  onOrderUpdate?: (order: OrderResultDto) => void;
}

export function useTradingHub({
  onSpotQuote,
  onOptionChain,
  onPcrMetrics,
  onStrategyScore,
  onPositions,
  onSemiAutoAlert,
  onOrderUpdate,
}: UseTradingHubProps) {
  const [isConnected, setIsConnected] = useState(false);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const hubRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const hub = new HubConnectionBuilder()
      .withUrl('/hubs/trading')
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();

    hubRef.current = hub;

    hub.on('ReceiveSpotQuote', (quote) => {
      onSpotQuote?.(quote);
    });

    hub.on('ReceiveOptionChain', (chain: OptionChainDto) => {
      onOptionChain?.(chain);
    });

    hub.on('ReceivePcrMetrics', (metrics: { pcr: PcrSummaryDto; maxPain: MaxPainDto }) => {
      onPcrMetrics?.(metrics);
    });

    hub.on('ReceiveStrategyScore', (score: FnOStrategyScoreDto) => {
      onStrategyScore?.(score);
    });

    hub.on('ReceivePositions', (data: { positions: PositionDto[]; totalUnrealizedPnl: number; todayRealizedPnl: number }) => {
      onPositions?.(data);
    });

    hub.on('ReceiveSemiAutoAlert', (alert: FnOStrategyScoreDto) => {
      onSemiAutoAlert?.(alert);
    });

    hub.on('ReceiveOrderUpdate', (order: OrderResultDto) => {
      onOrderUpdate?.(order);
    });

    hub.onreconnecting(() => {
      setIsConnected(false);
    });

    hub.onreconnected(() => {
      setIsConnected(true);
      setConnectionError(null);
    });

    hub.onclose(() => {
      setIsConnected(false);
    });

    async function startConnection() {
      try {
        if (hub.state === HubConnectionState.Disconnected) {
          await hub.start();
          setIsConnected(true);
          setConnectionError(null);
        }
      } catch (err: any) {
        setConnectionError(err.message || 'SignalR connection failed');
        setIsConnected(false);
      }
    }

    startConnection();

    return () => {
      hub.stop();
    };
  }, []);

  return { isConnected, connectionError };
}
