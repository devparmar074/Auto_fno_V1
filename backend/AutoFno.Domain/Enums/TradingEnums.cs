namespace AutoFno.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Submitted = 1,
    Complete = 2,
    Rejected = 3,
    Cancelled = 4,
    Failed = 5
}

public enum OrderType
{
    MARKET = 0,
    LIMIT = 1,
    SL = 2,
    SL_M = 3
}

public enum TransactionType
{
    BUY = 0,
    SELL = 1
}

public enum ProductType
{
    I = 0, // Intraday (MIS)
    D = 1  // Delivery / Normal (CNC/NRML)
}
