# AutoTrade FnO: NIFTY 50 & SENSEX Quantitative Trading Workstation

**AutoTrade FnO** is an institutional-grade, real-time algorithmic trading and quantitative analytics workstation engineered specifically for Indian Index Futures & Options (**NSE NIFTY 50** and **BSE SENSEX**).

Built on **.NET 10 (C#)**, **SignalR WebSockets**, and **React 19 + TypeScript + Tailwind CSS v4**, the workstation delivers real-time options chain depth, analytical European Black-Scholes Greeks, Put-Call Ratio (PCR) analytics, Max Pain strike calculation, strike-wise Open Interest (OI) buildup classification, and a 4-pillar quantitative conviction scoring engine. The interface is styled with the **OrderBlock Quantum Dark** design system featuring micro-animations across every element.

---

## Key Capabilities

### 1. Dual Index Support (NIFTY 50 & SENSEX)
- **Instant Index Switching**: Toggle seamlessly between **NSE NIFTY 50** (`NSE_INDEX|Nifty 50`, 50-point strike increments) and **BSE SENSEX** (`BSE_INDEX|SENSEX`, 100-point strike increments).
- **Automated Expiry Discovery**: Discovers all active weekly and monthly expiries directly from Upstox contracts metadata.
- **24/7 Simulator Fallback**: Automatically activates a tick simulator when testing after market hours or without active broker credentials.

### 2. Analytical Black-Scholes Greeks Engine
- **Internal European Analytical Engine**: Zero-latency calculation of Delta ($\Delta$), Gamma ($\Gamma$), Theta ($\Theta$ per day), Vega ($\nu$ per 1%), Rho ($\rho$), and Probability of Profit (POP).
- **High-Precision IV Solver**: Hybrid Newton-Raphson & Bisection Implied Volatility solver calibrated for Indian index options.
- **Dual-Source Architecture**: Uses live broker Greeks or falls back to the internal Black-Scholes engine when broker Greeks are unavailable.

### 3. Open Interest (OI), PCR & Buildup Analytics
- **Continuous PCR Sentiment**: Strike-wise OI PCR, Volume PCR, and At-The-Money (ATM) PCR.
- **Max Pain Theory**: Calculates the exact strike minimizing option sellers' total payout.
- **Buildup Classification**: Real-time tagging for every strike:
  - **LB**: Long Buildup (Price $\uparrow$, OI $\uparrow$) — Bullish accumulation
  - **SB**: Short Buildup (Price $\downarrow$, OI $\uparrow$) — Bearish writing
  - **SC**: Short Covering (Price $\uparrow$, OI $\downarrow$) — Short exit rally
  - **LU**: Long Unwinding (Price $\downarrow$, OI $\downarrow$) — Long exit liquidation
- **Key Liquidity Walls**: Identifies the Call Wall (Major Resistance) and Put Wall (Major Support).

### 4. 4-Pillar Quantitative Conviction Scoring (-100 to +100)
Combines market dynamics into an actionable confluence score:
1. **Pillar 1: PCR Sentiment Score (25 pts)**: Proportional continuous scoring anchored around the 1.00 baseline, coupled with intraday Volume PCR flow, ATM PCR, and PCR slope/velocity.
2. **Pillar 2: OI Distribution & Max Pain Gravity (25 pts)**: Net buildup dominance (LB + SC vs SB + LU), fresh call vs put writing flow, and distance gravity from Max Pain.
3. **Pillar 3: Option Greeks & IV Regime (25 pts)**: Decoupled evaluation of ATM Delta asymmetry micro-skew, IV regime vs trend momentum, Call vs Put volatility skew, and Gamma acceleration.
4. **Pillar 4: Spot Momentum & Trend (25 pts)**: EMA 9 / EMA 21 triad, Supertrend direction, and RSI-14 momentum bands.

High-conviction thresholds: $\ge +60$ for Bullish setups (**Buy ATM CE**) and $\le -60$ for Bearish setups (**Buy ATM PE**).

### 5. Tri-Mode Execution & Capital Protection
- **Manual Mode**: Visual cockpit with 1-click execution for Calls and Puts.
- **Semi-Auto Mode**: Evaluates setups continuously and displays modal alerts with pre-calculated lot sizes, entry prices, stop-losses, and profit targets.
- **Full-Auto Mode**: Automated bot execution with predefined risk guardrails.
- **Risk Management**: Enforces lot sizes (NIFTY: 25/75, SENSEX: 10/20), Max Daily Loss Limit (₹5,000 default), 03:15 PM IST auto square-off, and **Emergency Kill Switch**.

---

## OrderBlock Quantum Dark Design System

The workstation features a high-tech **OrderBlock Quantum Dark** aesthetic with reactive micro-animations:

- **Deep Void Canvas (`#06080C`)**: Frosted glassmorphism panels (`#0D1118`, `#11161F`, `#161C27`), 56px subtle quantum grid pattern, and ambient light blooms.
- **Neon Accents**: Bull (`#19E58C`), Bear (`#FF4D67`), Amber (`#FFB020`), Sky (`#45B8FF`), Teal (`#2DD4BF`), and Violet (`#7B7BFF`).
- **Continuous Radar Scanner Beams (`.ob-scanner-beam`)**: Sweeping holographic beams across all card headers.
- **Real-Time Liquidity Stream & Traveling Spark (`.ob-liq-line` + `.ob-liq-spark`)**: Dynamic order flow visualization in the PCR strip.
- **Pulsing At-The-Money (ATM) Highlight (`.ob-atm-row`)**: Continuous breathing highlight with glowing borders on the active ATM strike row.
- **Metallic Glint Shimmer Bars (`.ob-glint-bar`)**: 45-degree diagonal glare sweep across conviction progress bars and Greeks decay meters.
- **Breathing Conviction Card Halo (`.ob-glow-bull` / `.ob-glow-bear`)**: Perimeter halo adapting dynamically to conviction score thresholds.
- **Real-Time Spot Tick Flash (`.ob-flash-up` / `.ob-flash-down`)**: Flashes on spot prices and option strikes whenever ticks arrive via SignalR.
- **Sonar Pulse Dot (`.ob-sonar-dot`)**: Expanding radar ping indicators for WebSocket feed connectivity and broker authorization status.
- **TradingView Lightweight Charts (v5.2.1)**: Custom dark theme with neon candlesticks, EMA 9 (`#45B8FF`), and EMA 21 (`#FFB020`).

---

## Port & Isolation Architecture

Engineered to operate alongside `Auto_Trade_V1` with zero port collisions:

| Component | AutoTrade FnO (This Project) | Auto_Trade_V1 (Equities) | Status |
| :--- | :--- | :--- | :--- |
| **Backend API** | `http://localhost:5010` | `http://localhost:5000` | Isolated |
| **Backend HTTPS** | `https://localhost:5011` | `https://localhost:5001` | Isolated |
| **Frontend Web** | `http://localhost:5174` | `http://localhost:5173` | Isolated |
| **SignalR Hub** | `/hubs/trading` | `/tradingHub` | Isolated |
| **Database** | `AutoFnoDb` (LocalDB) | `AutoTradeDb` (LocalDB) | Isolated |

---

## Project Structure

```text
Auto_fno_V1/
├── backend/
│   ├── AutoFno.slnx
│   ├── AutoFno.Domain/          # Enums, Entities, DTOs & Models
│   ├── AutoFno.Application/     # Black-Scholes Greeks, PCR, Max Pain, Strategy Engine, Risk Manager
│   ├── AutoFno.Infrastructure/  # Upstox v2 Client, SignalR Hub, Dapper Repos, Background Services
│   └── AutoFno.Api/             # ASP.NET Core (.NET 10) Controllers, Swagger, Middlewares (Port 5010)
├── web/
│   └── auto-fno-web/            # React 19 + TypeScript + Vite + Tailwind CSS v4 Cockpit (Port 5174)
│       └── src/
│           ├── components/      # HeaderBar, OptionChainMatrix, StrategyConvictionCard, TradingViewChart, etc.
│           ├── styles/          # orderblock-animations.css (Quantum Dark keyframes & tokens)
│           └── services/        # SignalR & REST API services
├── database/
│   ├── Scripts/                 # 00_InitDatabase.sql (Tables, Stored Procedures, Seed Config)
│   ├── Tables/
│   └── StoredProcedures/
├── run-backend.ps1              # Launch .NET 10 API backend (Port 5010)
├── run-web.ps1                  # Launch React 19 web cockpit (Port 5174)
└── start-all.ps1                # One-click dual-launcher for backend + frontend
```

---

## Quickstart

### 1. Database Initialization (SQL Server LocalDB)
Ensure SQL Server LocalDB is running, then execute the initialization script:
```powershell
sqllocaldb start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i "database\Scripts\00_InitDatabase.sql"
```

### 2. Configure Upstox Developer Credentials (Optional)
Edit `backend/AutoFno.Api/appsettings.json` with your Upstox credentials:
```json
{
  "Upstox": {
    "ApiKey": "YOUR_UPSTOX_API_KEY",
    "ApiSecret": "YOUR_UPSTOX_API_SECRET",
    "RedirectUri": "http://localhost:5010/api/upstoxauth/callback",
    "BaseUrl": "https://api.upstox.com/v2"
  }
}
```
*(If no credentials are provided or the market is closed, the platform automatically runs on its built-in realistic simulator).*

### 3. Launch Services

**Option A: One-Click Dual Launcher (Recommended)**
```powershell
.\start-all.ps1
```

**Option B: Individual Launchers**
- Backend:
  ```powershell
  .\run-backend.ps1
  ```
- Frontend:
  ```powershell
  .\run-web.ps1
  ```

### 4. Access URLs
- **Web Cockpit UI**: [http://localhost:5174](http://localhost:5174)
- **Swagger API Documentation**: [http://localhost:5010/swagger](http://localhost:5010/swagger)
- **SignalR Trading Hub**: `http://localhost:5010/hubs/trading`

---

## Trading Workflow

1. Open **[http://localhost:5174](http://localhost:5174)** in your browser.
2. Select **NIFTY 50** or **SENSEX** and choose the desired expiry from the dropdown.
3. Observe live spot ticks, ATM pulse glow, liquidity stream, option chain buildup badges, and Greeks.
4. When the 4-Pillar Quantitative Score reaches $\ge +60$ or $\le -60$, review the setup recommendation card.
5. In **Manual** or **Semi-Auto** mode, review strike, lot size, entry price, stop-loss, and target.
6. Verify and confirm the order via the confirmation modal.
7. Monitor open positions, live MTM P&L, or use **1-Click Square-Off** or the **Emergency Kill Switch** at any time.
