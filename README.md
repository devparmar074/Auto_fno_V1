# AutoTrade FnO: NIFTY 50 & SENSEX Quantitative Trading Workstation

**AutoTrade FnO** is a high-performance, real-time algorithmic trading and quantitative analysis workstation engineered specifically for Indian Index Futures & Options (**NSE NIFTY 50** and **BSE SENSEX**).

Built on **.NET 10 (C#)**, **SignalR WebSockets**, and **React 19 + Vite (TypeScript)**, it connects directly with **Upstox API v2** to calculate pure mathematical indicators, full European Option Greeks, Put-Call Ratio (PCR) analytics, Strike-wise Open Interest (OI) buildup, and Max Pain levels on-the-fly.

---

## Key Capabilities

1. **Dual Index Support (NIFTY 50 & SENSEX)**:
   - Dynamic switching between **NSE NIFTY 50** (`NSE_INDEX|Nifty 50`, 50-pt strike intervals) and **BSE SENSEX** (`BSE_INDEX|SENSEX`, 100-pt strike intervals).
   - Automated weekly & monthly expiry discovery from Upstox contracts endpoint.
2. **Pure Mathematical Black-Scholes Greeks Engine**:
   - Internal analytical engine calculating Delta ($\Delta$), Gamma ($\Gamma$), Theta ($\Theta$ per day), Vega ($\nu$ per 1%), Rho ($\rho$), and Probability of Profit (POP).
   - High-precision Newton-Raphson & Bisection Implied Volatility (IV) solver.
   - Dual-source architecture: Uses live broker Greeks or falls back to internal pure math calculation for zero-delay pricing.
3. **Deep Open Interest (OI) & PCR Analytics**:
   - **Open Interest PCR**: Identifies institutional Put writing vs Call writing pressure.
   - **Volume PCR & ATM PCR**: Evaluates immediate liquidity concentration around spot.
   - **Max Pain Theory**: Calculates the exact strike price where option sellers maximize profit.
   - **Strike Buildup Classification**: Automatically tags every strike with *Long Buildup (LB)*, *Short Buildup (SB)*, *Short Covering (SC)*, or *Long Unwinding (LU)*.
   - **Key Walls**: Identifies Call Wall (Major Resistance) and Put Wall (Major Support).
4. **4-Pillar Quantitative Conviction Scoring (-100 to +100)**:
   - Aggregates market dynamics into a single high-conviction decision score:
     * **PCR Sentiment Pillar (25 pts)**
     * **OI Distribution & Max Pain Gravity Pillar (25 pts)**
     * **Option Greeks & IV Regime Pillar (25 pts)**
     * **Spot Index Momentum & EMA Triad Pillar (25 pts)**
   - High-conviction threshold $\ge +60$ for Bullish setups (Buy ATM CE) and $\le -60$ for Bearish setups (Buy ATM PE).
5. **Tri-Mode Execution & Capital Protection**:
   - **Manual Mode**: Visual terminal with interactive 1-click execution.
   - **Semi-Auto Mode**: Evaluates setups continuously and pops up interactive execution alerts with pre-calculated quantities and stop-loss/take-profit targets.
   - **Full-Auto Mode**: Algorithmic bot execution directly through Upstox API v2.
   - **Risk Enforcement**: Lot size controls (NIFTY: 25/75, SENSEX: 10/20), Max Daily Loss Limit (₹5,000 default), 03:15 PM IST auto square-off, and **Emergency Kill Switch**.
6. **24/7 Simulator Fallback**:
   - Built-in market simulator providing realistic tick-by-tick option chains and spot feeds when testing after market hours or without live broker credentials.

---

## Project Structure

```text
Auto_fno_V1/
├── backend/
│   ├── AutoFno.slnx
│   ├── AutoFno.Domain/          # Enums, Entities, DTOs & Models
│   ├── AutoFno.Application/     # Black-Scholes Greeks, PCR, Max Pain, Risk Manager, Interfaces
│   ├── AutoFno.Infrastructure/  # Upstox v2 Client, SignalR Hub, Dapper Repos, Background Workers
│   └── AutoFno.Api/             # ASP.NET Core (.NET 10) Controllers, Swagger, Middlewares
├── web/
│   └── auto-fno-web/            # React 19 + TypeScript + Vite + Tailwind CSS UI
├── database/
│   ├── Scripts/                 # 00_InitDatabase.sql (Tables, Procedures, Seed Data)
│   ├── Tables/
│   └── StoredProcedures/
├── run-backend.ps1              # Launch .NET 10 API backend
├── run-web.ps1                  # Launch React + Vite web cockpit
└── start-all.ps1                # One-click ecosystem launcher
```

---

## Quickstart

### 1. Database Initialization (SQL Server LocalDB)
Ensure SQL Server LocalDB is running:
```powershell
sqllocaldb start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i "database\Scripts\00_InitDatabase.sql"
```

### 2. Configure Upstox Developer Credentials
Open `backend/AutoFno.Api/appsettings.json` and enter your Upstox Developer App details:
```json
{
  "Upstox": {
    "ApiKey": "YOUR_UPSTOX_API_KEY",
    "ApiSecret": "YOUR_UPSTOX_API_SECRET",
    "RedirectUri": "http://localhost:5000/api/upstoxauth/callback"
  }
}
```

### 3. Launch Backend API
```powershell
.\run-backend.ps1
```
* Backend API: `http://localhost:5000` & `https://localhost:5001`
* Swagger UI: `http://localhost:5000/swagger`
* SignalR Hub: `http://localhost:5000/hubs/trading`

### 4. Launch Web Terminal
```powershell
.\run-web.ps1
```
Open your browser at `http://localhost:5173`.

### 5. Launch All with One Click
```powershell
.\start-all.ps1
```

---

## Trading Workflow

1. Open `http://localhost:5173`.
2. Toggle between **NIFTY 50** and **SENSEX** or select an upcoming weekly expiry.
3. Observe live tick pulse animations, Option Chain matrix, PCR metrics, and Greeks matrix.
4. When conviction crosses $\ge +60$ or $\le -60$, inspect the recommendation setup card.
5. In **Semi-Auto** or **Manual** mode, review strike, lot size, entry price, stop-loss, and target.
6. Check **"Confirm live execution"** safety prompt and click **Confirm Live Order**.
7. Monitor live open positions, real-time MTM P&L, or use **1-Click Square-Off** or **Emergency Kill Switch** at any time.
