-- ==============================================================================
-- AutoFno V2.0 Database Tables for Maturity Levels 4 to 9
-- Fully non-destructive, additive tables for persistent shadow trading,
-- forward validation campaigns, and machine learning governance.
-- ==============================================================================

USE [AutoFnoDb];
GO

-- 1. Table: V2ShadowPosition (Persistent storage for active shadow positions)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V2ShadowPosition')
BEGIN
    CREATE TABLE dbo.V2ShadowPosition (
        PositionId UNIQUEIDENTIFIER PRIMARY KEY,
        IndexSymbol NVARCHAR(50) NOT NULL,
        InstrumentKey NVARCHAR(100) NOT NULL,
        TradingSymbol NVARCHAR(100) NOT NULL,
        OptionType INT NOT NULL,
        TargetStrike DECIMAL(18,4) NOT NULL,
        Lots INT NOT NULL,
        Quantity INT NOT NULL,
        RemainingQuantity INT NOT NULL,
        EntryPrice DECIMAL(18,4) NOT NULL,
        CurrentPrice DECIMAL(18,4) NOT NULL,
        StopLossPrice DECIMAL(18,4) NOT NULL,
        Target1Price DECIMAL(18,4) NOT NULL,
        Target2Price DECIMAL(18,4) NOT NULL,
        InitialRiskPerLot DECIMAL(18,4) NOT NULL,
        IsPartiallyExited BIT NOT NULL DEFAULT 0,
        UnrealizedPnl DECIMAL(18,4) NOT NULL DEFAULT 0,
        RealizedPnl DECIMAL(18,4) NOT NULL DEFAULT 0,
        PeakPrice DECIMAL(18,4) NOT NULL,
        TroughPrice DECIMAL(18,4) NOT NULL,
        EntryTimeUtc DATETIME2 NOT NULL,
        LastUpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsOpen BIT NOT NULL DEFAULT 1,
        ExitReason INT NOT NULL DEFAULT 0
    );
    CREATE NONCLUSTERED INDEX IX_V2ShadowPosition_IsOpen ON dbo.V2ShadowPosition(IsOpen);
    CREATE NONCLUSTERED INDEX IX_V2ShadowPosition_IndexSymbol ON dbo.V2ShadowPosition(IndexSymbol);
END
GO

-- 2. Table: V2ShadowTrade (Audit record of closed shadow paper trades)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V2ShadowTrade')
BEGIN
    CREATE TABLE dbo.V2ShadowTrade (
        TradeId UNIQUEIDENTIFIER PRIMARY KEY,
        IndexSymbol NVARCHAR(50) NOT NULL,
        TradingSymbol NVARCHAR(100) NOT NULL,
        OptionType INT NOT NULL,
        Lots INT NOT NULL,
        Quantity INT NOT NULL,
        EntryPrice DECIMAL(18,4) NOT NULL,
        ExitPrice DECIMAL(18,4) NOT NULL,
        GrossPnl DECIMAL(18,4) NOT NULL,
        EstimatedCosts DECIMAL(18,4) NOT NULL,
        NetPnl DECIMAL(18,4) NOT NULL,
        RMultipleAchieved DECIMAL(18,4) NOT NULL,
        HoldingMinutes FLOAT NOT NULL,
        MaePercent DECIMAL(18,4) NOT NULL,
        MfePercent DECIMAL(18,4) NOT NULL,
        ExitReason INT NOT NULL,
        EntryConvictionScore DECIMAL(18,4) NOT NULL,
        ScoreBucket NVARCHAR(50) NOT NULL,
        EntryTimeUtc DATETIME2 NOT NULL,
        ExitTimeUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE NONCLUSTERED INDEX IX_V2ShadowTrade_ScoreBucket ON dbo.V2ShadowTrade(ScoreBucket);
    CREATE NONCLUSTERED INDEX IX_V2ShadowTrade_ExitTimeUtc ON dbo.V2ShadowTrade(ExitTimeUtc);
END
GO

-- 3. Table: V2ForwardValidationCampaign (Tracks 30-day forward validation runs)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V2ForwardValidationCampaign')
BEGIN
    CREATE TABLE dbo.V2ForwardValidationCampaign (
        CampaignId UNIQUEIDENTIFIER PRIMARY KEY,
        BaselineVersion NVARCHAR(50) NOT NULL DEFAULT 'v2.0-baseline',
        Symbol NVARCHAR(50) NOT NULL DEFAULT 'NIFTY50',
        Status INT NOT NULL DEFAULT 0,
        StartDateUtc DATETIME2 NOT NULL,
        TargetEndDateUtc DATETIME2 NOT NULL,
        ElapsedCalendarDays INT NOT NULL DEFAULT 0,
        ElapsedMarketSessions INT NOT NULL DEFAULT 0,
        TotalSignalsObserved INT NOT NULL DEFAULT 0,
        TotalTradesRecorded INT NOT NULL DEFAULT 0,
        WinRatePercent DECIMAL(18,4) NOT NULL DEFAULT 0,
        NetPnl DECIMAL(18,4) NOT NULL DEFAULT 0,
        NetExpectancy DECIMAL(18,4) NOT NULL DEFAULT 0,
        ProfitFactor DECIMAL(18,4) NOT NULL DEFAULT 0,
        MaxDrawdownPercent DECIMAL(18,4) NOT NULL DEFAULT 0,
        MonotonicityScore DECIMAL(18,4) NOT NULL DEFAULT 0,
        HasPassedAllGates BIT NOT NULL DEFAULT 0,
        ReportSummary NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE NONCLUSTERED INDEX IX_V2ForwardValidationCampaign_Status ON dbo.V2ForwardValidationCampaign(Status);
END
GO

-- 4. Table: V2MLModelGovernance (Tracks ML models, weights, versions, and rollback states)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'V2MLModelGovernance')
BEGIN
    CREATE TABLE dbo.V2MLModelGovernance (
        ModelId UNIQUEIDENTIFIER PRIMARY KEY,
        Version NVARCHAR(100) NOT NULL UNIQUE,
        Status INT NOT NULL DEFAULT 0,
        Objective INT NOT NULL DEFAULT 0,
        InSampleProfitFactor DECIMAL(18,4) NOT NULL DEFAULT 0,
        OutOfSampleProfitFactor DECIMAL(18,4) NOT NULL DEFAULT 0,
        InSampleExpectancy DECIMAL(18,4) NOT NULL DEFAULT 0,
        OutOfSampleExpectancy DECIMAL(18,4) NOT NULL DEFAULT 0,
        WalkForwardEfficiencyRatio DECIMAL(18,4) NOT NULL DEFAULT 0,
        WeightPcr DECIMAL(18,4) NOT NULL DEFAULT 1.0,
        WeightOi DECIMAL(18,4) NOT NULL DEFAULT 1.0,
        WeightGreeks DECIMAL(18,4) NOT NULL DEFAULT 1.0,
        WeightSpotTrend DECIMAL(18,4) NOT NULL DEFAULT 1.0,
        MinConvictionThreshold DECIMAL(18,4) NOT NULL DEFAULT 60.0,
        IvCrushThreshold DECIMAL(18,4) NOT NULL DEFAULT 85.0,
        FeatureImportancesJson NVARCHAR(MAX) NULL,
        GovernanceNotes NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ActivatedAtUtc DATETIME2 NULL,
        RolledBackAtUtc DATETIME2 NULL
    );
    CREATE NONCLUSTERED INDEX IX_V2MLModelGovernance_Status ON dbo.V2MLModelGovernance(Status);
END
GO
