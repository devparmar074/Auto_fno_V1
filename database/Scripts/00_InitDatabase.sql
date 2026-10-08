-- ==============================================================================
-- AutoFno Database Initialization Script
-- Supports NSE NIFTY 50 and BSE SENSEX Futures & Options Trading
-- ==============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AutoFnoDb')
BEGIN
    CREATE DATABASE [AutoFnoDb];
END
GO

USE [AutoFnoDb];
GO

-- 1. Table: UpstoxConnection
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UpstoxConnection')
BEGIN
    CREATE TABLE dbo.UpstoxConnection (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EncryptedAccessToken NVARCHAR(MAX) NOT NULL,
        EncryptedRefreshToken NVARCHAR(MAX) NULL,
        ExpiresAtUtc DATETIME2 NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        UserId NVARCHAR(100) NULL,
        UserName NVARCHAR(200) NULL,
        Email NVARCHAR(200) NULL,
        PrimaryIp NVARCHAR(50) NULL,
        Broker NVARCHAR(50) NOT NULL DEFAULT 'UPSTOX',
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE NONCLUSTERED INDEX IX_UpstoxConnection_IsActive ON dbo.UpstoxConnection(IsActive);
END
GO

-- 2. Table: Instrument
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Instrument')
BEGIN
    CREATE TABLE dbo.Instrument (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InstrumentKey NVARCHAR(100) NOT NULL UNIQUE,
        TradingSymbol NVARCHAR(100) NOT NULL,
        CompanyName NVARCHAR(200) NOT NULL,
        Exchange NVARCHAR(20) NOT NULL,
        Segment NVARCHAR(20) NOT NULL,
        UnderlyingKey NVARCHAR(100) NULL,
        UnderlyingSymbol NVARCHAR(100) NULL,
        StrikePrice DECIMAL(18,4) NULL,
        OptionType NVARCHAR(10) NULL, -- CE or PE
        ExpiryDate DATETIME2 NULL,
        LotSize INT NOT NULL DEFAULT 1,
        TickSize DECIMAL(18,4) NOT NULL DEFAULT 0.05,
        FreezeQuantity DECIMAL(18,4) NOT NULL DEFAULT 1800,
        ExchangeToken NVARCHAR(50) NULL,
        LastUpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE NONCLUSTERED INDEX IX_Instrument_UnderlyingKey ON dbo.Instrument(UnderlyingKey);
    CREATE NONCLUSTERED INDEX IX_Instrument_TradingSymbol ON dbo.Instrument(TradingSymbol);
END
GO

-- 3. Table: TradeOrder
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TradeOrder')
BEGIN
    CREATE TABLE dbo.TradeOrder (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        CorrelationId NVARCHAR(100) NOT NULL UNIQUE,
        BrokerOrderId NVARCHAR(100) NULL,
        InstrumentKey NVARCHAR(100) NOT NULL,
        TradingSymbol NVARCHAR(100) NOT NULL,
        IndexSymbol NVARCHAR(50) NOT NULL DEFAULT 'NIFTY50',
        StrikePrice DECIMAL(18,4) NULL,
        OptionType INT NULL,
        TransactionType INT NOT NULL, -- 0=BUY, 1=SELL
        OrderType INT NOT NULL,        -- 0=MARKET, 1=LIMIT, 2=SL
        ProductType INT NOT NULL,      -- 0=I (MIS), 1=D (NRML)
        Quantity INT NOT NULL,
        Lots INT NOT NULL DEFAULT 1,
        Price DECIMAL(18,4) NOT NULL,
        TriggerPrice DECIMAL(18,4) NOT NULL DEFAULT 0,
        StopLossPrice DECIMAL(18,4) NULL,
        TakeProfitPrice DECIMAL(18,4) NULL,
        Status INT NOT NULL,           -- 0=Pending, 1=Submitted, 2=Complete, 3=Rejected, 4=Cancelled, 5=Failed
        StatusMessage NVARCHAR(500) NULL,
        ExecutedPrice DECIMAL(18,4) NOT NULL DEFAULT 0,
        FilledQuantity INT NOT NULL DEFAULT 0,
        PlacedByMode INT NOT NULL DEFAULT 0, -- 0=Manual, 1=SemiAuto, 2=FullAuto
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ExecutedAtUtc DATETIME2 NULL
    );
    CREATE NONCLUSTERED INDEX IX_TradeOrder_BrokerOrderId ON dbo.TradeOrder(BrokerOrderId);
    CREATE NONCLUSTERED INDEX IX_TradeOrder_Status ON dbo.TradeOrder(Status);
    CREATE NONCLUSTERED INDEX IX_TradeOrder_CreatedAtUtc ON dbo.TradeOrder(CreatedAtUtc);
END
GO

-- 4. Table: TradeFill
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TradeFill')
BEGIN
    CREATE TABLE dbo.TradeFill (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        OrderId BIGINT NOT NULL FOREIGN KEY REFERENCES dbo.TradeOrder(Id),
        FillId NVARCHAR(100) NULL,
        FillPrice DECIMAL(18,4) NOT NULL,
        FillQuantity INT NOT NULL,
        FilledAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE NONCLUSTERED INDEX IX_TradeFill_OrderId ON dbo.TradeFill(OrderId);
END
GO

-- 5. Table: PositionSnapshot
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PositionSnapshot')
BEGIN
    CREATE TABLE dbo.PositionSnapshot (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        InstrumentKey NVARCHAR(100) NOT NULL,
        TradingSymbol NVARCHAR(100) NOT NULL,
        IndexSymbol NVARCHAR(50) NOT NULL DEFAULT 'NIFTY50',
        StrikePrice DECIMAL(18,4) NULL,
        OptionType INT NULL,
        Quantity INT NOT NULL,
        Lots INT NOT NULL DEFAULT 1,
        AveragePrice DECIMAL(18,4) NOT NULL,
        CurrentLtp DECIMAL(18,4) NOT NULL,
        UnrealizedPnl DECIMAL(18,4) NOT NULL,
        RealizedPnl DECIMAL(18,4) NOT NULL,
        SnapshotTimeUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE NONCLUSTERED INDEX IX_PositionSnapshot_InstrumentKey ON dbo.PositionSnapshot(InstrumentKey);
    CREATE NONCLUSTERED INDEX IX_PositionSnapshot_SnapshotTimeUtc ON dbo.PositionSnapshot(SnapshotTimeUtc);
END
GO

-- 6. Table: BotConfig
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BotConfig')
BEGIN
    CREATE TABLE dbo.BotConfig (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Mode INT NOT NULL DEFAULT 0, -- 0=Manual, 1=SemiAuto, 2=FullAuto
        ActiveIndex NVARCHAR(50) NOT NULL DEFAULT 'NIFTY50',
        DefaultLots INT NOT NULL DEFAULT 1,
        MaxDailyLossLimit DECIMAL(18,4) NOT NULL DEFAULT 5000.0,
        MaxDailyProfitTarget DECIMAL(18,4) NOT NULL DEFAULT 10000.0,
        StopLossPercent DECIMAL(18,4) NOT NULL DEFAULT 20.0,
        TakeProfitPercent DECIMAL(18,4) NOT NULL DEFAULT 40.0,
        MaxOpenPositions INT NOT NULL DEFAULT 2,
        IsKillSwitchActive BIT NOT NULL DEFAULT 0,
        AutoSquareOffAtCutoff BIT NOT NULL DEFAULT 1,
        AutoSquareOffTimeIst NVARCHAR(10) NOT NULL DEFAULT '15:15:00',
        UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );

    -- Seed initial default configuration
    INSERT INTO dbo.BotConfig (Mode, ActiveIndex, DefaultLots, MaxDailyLossLimit, MaxDailyProfitTarget, StopLossPercent, TakeProfitPercent, MaxOpenPositions, IsKillSwitchActive, AutoSquareOffAtCutoff, AutoSquareOffTimeIst, UpdatedAtUtc)
    VALUES (0, 'NIFTY50', 1, 5000.0, 10000.0, 20.0, 40.0, 2, 0, 1, '15:15:00', SYSUTCDATETIME());
END
GO

-- 7. Table: StrategySignalHistory
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StrategySignalHistory')
BEGIN
    CREATE TABLE dbo.StrategySignalHistory (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        IndexSymbol NVARCHAR(50) NOT NULL DEFAULT 'NIFTY50',
        SpotPrice DECIMAL(18,4) NOT NULL,
        TotalScore DECIMAL(18,4) NOT NULL,
        PcrScore DECIMAL(18,4) NOT NULL,
        OiMaxPainScore DECIMAL(18,4) NOT NULL,
        GreeksIvScore DECIMAL(18,4) NOT NULL,
        SpotTrendScore DECIMAL(18,4) NOT NULL,
        Recommendation INT NOT NULL,
        RecommendedStrike NVARCHAR(100) NULL,
        RecommendedOptionType INT NULL,
        SetupRationale NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE NONCLUSTERED INDEX IX_StrategySignalHistory_IndexSymbol ON dbo.StrategySignalHistory(IndexSymbol);
    CREATE NONCLUSTERED INDEX IX_StrategySignalHistory_CreatedAtUtc ON dbo.StrategySignalHistory(CreatedAtUtc);
END
GO

-- ==============================================================================
-- Stored Procedures
-- ==============================================================================

-- sp_SaveUpstoxConnection
CREATE OR ALTER PROCEDURE dbo.sp_SaveUpstoxConnection
    @EncryptedAccessToken NVARCHAR(MAX),
    @EncryptedRefreshToken NVARCHAR(MAX) = NULL,
    @ExpiresAtUtc DATETIME2,
    @UserId NVARCHAR(100) = NULL,
    @UserName NVARCHAR(200) = NULL,
    @Email NVARCHAR(200) = NULL,
    @PrimaryIp NVARCHAR(50) = NULL,
    @Broker NVARCHAR(50) = 'UPSTOX'
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UpstoxConnection SET IsActive = 0 WHERE IsActive = 1;
    INSERT INTO dbo.UpstoxConnection (EncryptedAccessToken, EncryptedRefreshToken, ExpiresAtUtc, IsActive, UserId, UserName, Email, PrimaryIp, Broker, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@EncryptedAccessToken, @EncryptedRefreshToken, @ExpiresAtUtc, 1, @UserId, @UserName, @Email, @PrimaryIp, @Broker, SYSUTCDATETIME(), SYSUTCDATETIME());
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- sp_GetActiveUpstoxConnection
CREATE OR ALTER PROCEDURE dbo.sp_GetActiveUpstoxConnection
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM dbo.UpstoxConnection WHERE IsActive = 1 AND ExpiresAtUtc > SYSUTCDATETIME() ORDER BY Id DESC;
END
GO

-- sp_DeactivateUpstoxConnection
CREATE OR ALTER PROCEDURE dbo.sp_DeactivateUpstoxConnection
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.UpstoxConnection SET IsActive = 0, UpdatedAtUtc = SYSUTCDATETIME() WHERE IsActive = 1;
END
GO

-- sp_CreateTradeOrder
CREATE OR ALTER PROCEDURE dbo.sp_CreateTradeOrder
    @CorrelationId NVARCHAR(100),
    @BrokerOrderId NVARCHAR(100) = NULL,
    @InstrumentKey NVARCHAR(100),
    @TradingSymbol NVARCHAR(100),
    @IndexSymbol NVARCHAR(50),
    @StrikePrice DECIMAL(18,4) = NULL,
    @OptionType INT = NULL,
    @TransactionType INT,
    @OrderType INT,
    @ProductType INT,
    @Quantity INT,
    @Lots INT,
    @Price DECIMAL(18,4),
    @TriggerPrice DECIMAL(18,4) = 0,
    @StopLossPrice DECIMAL(18,4) = NULL,
    @TakeProfitPrice DECIMAL(18,4) = NULL,
    @Status INT,
    @StatusMessage NVARCHAR(500) = NULL,
    @ExecutedPrice DECIMAL(18,4) = 0,
    @FilledQuantity INT = 0,
    @PlacedByMode INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TradeOrder (CorrelationId, BrokerOrderId, InstrumentKey, TradingSymbol, IndexSymbol, StrikePrice, OptionType, TransactionType, OrderType, ProductType, Quantity, Lots, Price, TriggerPrice, StopLossPrice, TakeProfitPrice, Status, StatusMessage, ExecutedPrice, FilledQuantity, PlacedByMode, CreatedAtUtc)
    VALUES (@CorrelationId, @BrokerOrderId, @InstrumentKey, @TradingSymbol, @IndexSymbol, @StrikePrice, @OptionType, @TransactionType, @OrderType, @ProductType, @Quantity, @Lots, @Price, @TriggerPrice, @StopLossPrice, @TakeProfitPrice, @Status, @StatusMessage, @ExecutedPrice, @FilledQuantity, @PlacedByMode, SYSUTCDATETIME());
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- sp_UpdateTradeOrderStatus
CREATE OR ALTER PROCEDURE dbo.sp_UpdateTradeOrderStatus
    @OrderId BIGINT,
    @BrokerOrderId NVARCHAR(100) = NULL,
    @Status INT,
    @StatusMessage NVARCHAR(500) = NULL,
    @ExecutedPrice DECIMAL(18,4) = 0,
    @FilledQuantity INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TradeOrder
    SET BrokerOrderId = COALESCE(@BrokerOrderId, BrokerOrderId),
        Status = @Status,
        StatusMessage = COALESCE(@StatusMessage, StatusMessage),
        ExecutedPrice = CASE WHEN @ExecutedPrice > 0 THEN @ExecutedPrice ELSE ExecutedPrice END,
        FilledQuantity = CASE WHEN @FilledQuantity > 0 THEN @FilledQuantity ELSE FilledQuantity END,
        ExecutedAtUtc = CASE WHEN @Status IN (2, 3, 4, 5) THEN SYSUTCDATETIME() ELSE ExecutedAtUtc END
    WHERE Id = @OrderId;
END
GO

-- sp_GetOrderById
CREATE OR ALTER PROCEDURE dbo.sp_GetOrderById
    @OrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.TradeOrder WHERE Id = @OrderId;
END
GO

-- sp_GetOrderByCorrelationId
CREATE OR ALTER PROCEDURE dbo.sp_GetOrderByCorrelationId
    @CorrelationId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.TradeOrder WHERE CorrelationId = @CorrelationId;
END
GO

-- sp_GetRecentOrders
CREATE OR ALTER PROCEDURE dbo.sp_GetRecentOrders
    @Top INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top) * FROM dbo.TradeOrder ORDER BY Id DESC;
END
GO

-- sp_AddTradeFill
CREATE OR ALTER PROCEDURE dbo.sp_AddTradeFill
    @OrderId BIGINT,
    @FillId NVARCHAR(100) = NULL,
    @FillPrice DECIMAL(18,4),
    @FillQuantity INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TradeFill (OrderId, FillId, FillPrice, FillQuantity, FilledAtUtc)
    VALUES (@OrderId, @FillId, @FillPrice, @FillQuantity, SYSUTCDATETIME());
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- sp_SavePositionSnapshot
CREATE OR ALTER PROCEDURE dbo.sp_SavePositionSnapshot
    @InstrumentKey NVARCHAR(100),
    @TradingSymbol NVARCHAR(100),
    @IndexSymbol NVARCHAR(50),
    @StrikePrice DECIMAL(18,4) = NULL,
    @OptionType INT = NULL,
    @Quantity INT,
    @Lots INT,
    @AveragePrice DECIMAL(18,4),
    @CurrentLtp DECIMAL(18,4),
    @UnrealizedPnl DECIMAL(18,4),
    @RealizedPnl DECIMAL(18,4)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.PositionSnapshot (InstrumentKey, TradingSymbol, IndexSymbol, StrikePrice, OptionType, Quantity, Lots, AveragePrice, CurrentLtp, UnrealizedPnl, RealizedPnl, SnapshotTimeUtc)
    VALUES (@InstrumentKey, @TradingSymbol, @IndexSymbol, @StrikePrice, @OptionType, @Quantity, @Lots, @AveragePrice, @CurrentLtp, @UnrealizedPnl, @RealizedPnl, SYSUTCDATETIME());
END
GO

-- sp_GetLatestPositionSnapshots
CREATE OR ALTER PROCEDURE dbo.sp_GetLatestPositionSnapshots
AS
BEGIN
    SET NOCOUNT ON;
    WITH RankedPositions AS (
        SELECT *, ROW_NUMBER() OVER(PARTITION BY InstrumentKey ORDER BY SnapshotTimeUtc DESC) as rn
        FROM dbo.PositionSnapshot
    )
    SELECT * FROM RankedPositions WHERE rn = 1 AND Quantity <> 0;
END
GO

-- sp_GetBotConfig
CREATE OR ALTER PROCEDURE dbo.sp_GetBotConfig
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM dbo.BotConfig ORDER BY Id DESC;
END
GO

-- sp_UpdateBotConfig
CREATE OR ALTER PROCEDURE dbo.sp_UpdateBotConfig
    @Mode INT,
    @ActiveIndex NVARCHAR(50),
    @DefaultLots INT,
    @MaxDailyLossLimit DECIMAL(18,4),
    @MaxDailyProfitTarget DECIMAL(18,4),
    @StopLossPercent DECIMAL(18,4),
    @TakeProfitPercent DECIMAL(18,4),
    @MaxOpenPositions INT,
    @IsKillSwitchActive BIT,
    @AutoSquareOffAtCutoff BIT,
    @AutoSquareOffTimeIst NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.BotConfig
    SET Mode = @Mode,
        ActiveIndex = @ActiveIndex,
        DefaultLots = @DefaultLots,
        MaxDailyLossLimit = @MaxDailyLossLimit,
        MaxDailyProfitTarget = @MaxDailyProfitTarget,
        StopLossPercent = @StopLossPercent,
        TakeProfitPercent = @TakeProfitPercent,
        MaxOpenPositions = @MaxOpenPositions,
        IsKillSwitchActive = @IsKillSwitchActive,
        AutoSquareOffAtCutoff = @AutoSquareOffAtCutoff,
        AutoSquareOffTimeIst = @AutoSquareOffTimeIst,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Id = 1;
    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.BotConfig (Mode, ActiveIndex, DefaultLots, MaxDailyLossLimit, MaxDailyProfitTarget, StopLossPercent, TakeProfitPercent, MaxOpenPositions, IsKillSwitchActive, AutoSquareOffAtCutoff, AutoSquareOffTimeIst, UpdatedAtUtc)
        VALUES (@Mode, @ActiveIndex, @DefaultLots, @MaxDailyLossLimit, @MaxDailyProfitTarget, @StopLossPercent, @TakeProfitPercent, @MaxOpenPositions, @IsKillSwitchActive, @AutoSquareOffAtCutoff, @AutoSquareOffTimeIst, SYSUTCDATETIME());
    END
    SELECT TOP 1 * FROM dbo.BotConfig ORDER BY Id DESC;
END
GO

-- sp_SaveStrategySignal
CREATE OR ALTER PROCEDURE dbo.sp_SaveStrategySignal
    @IndexSymbol NVARCHAR(50),
    @SpotPrice DECIMAL(18,4),
    @TotalScore DECIMAL(18,4),
    @PcrScore DECIMAL(18,4),
    @OiMaxPainScore DECIMAL(18,4),
    @GreeksIvScore DECIMAL(18,4),
    @SpotTrendScore DECIMAL(18,4),
    @Recommendation INT,
    @RecommendedStrike NVARCHAR(100) = NULL,
    @RecommendedOptionType INT = NULL,
    @SetupRationale NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.StrategySignalHistory (IndexSymbol, SpotPrice, TotalScore, PcrScore, OiMaxPainScore, GreeksIvScore, SpotTrendScore, Recommendation, RecommendedStrike, RecommendedOptionType, SetupRationale, CreatedAtUtc)
    VALUES (@IndexSymbol, @SpotPrice, @TotalScore, @PcrScore, @OiMaxPainScore, @GreeksIvScore, @SpotTrendScore, @Recommendation, @RecommendedStrike, @RecommendedOptionType, @SetupRationale, SYSUTCDATETIME());
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- sp_GetRecentStrategySignals
CREATE OR ALTER PROCEDURE dbo.sp_GetRecentStrategySignals
    @IndexSymbol NVARCHAR(50) = NULL,
    @Top INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top) *
    FROM dbo.StrategySignalHistory
    WHERE @IndexSymbol IS NULL OR IndexSymbol = @IndexSymbol
    ORDER BY Id DESC;
END
GO
