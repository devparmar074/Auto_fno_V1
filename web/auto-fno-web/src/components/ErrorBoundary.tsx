import { Component, type ErrorInfo, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Uncaught error:', error, errorInfo);
  }

  public render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen bg-[#06080C] text-[#EDF2F9] flex items-center justify-center p-6 ob-grid-bg">
          <div className="max-w-md w-full ob-panel border border-[#FF4D67]/40 rounded-2xl p-6 text-center shadow-2xl ob-glow-bear">
            <div className="w-12 h-12 bg-[#FF4D67]/10 text-[#FF4D67] rounded-full flex items-center justify-center mx-auto mb-4 text-2xl font-bold border border-[#FF4D67]/30">
              !
            </div>
            <h2 className="text-xl font-bold text-white mb-2 font-mono ob-grad-bear">Terminal Execution Alert</h2>
            <p className="text-xs text-slate-400 mb-4 font-mono">
              An unexpected render error occurred in the trading terminal.
            </p>
            <div className="bg-[#06080C] p-3 rounded-lg text-xs text-[#FF4D67] font-mono text-left overflow-auto max-h-32 mb-6 border border-[#1A212D]">
              {this.state.error?.message || 'Unknown error'}
            </div>
            <button
              onClick={() => window.location.reload()}
              className="w-full ob-btn-bull text-white font-bold py-2.5 px-4 rounded-xl transition text-xs font-mono"
            >
              Reload Terminal
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
