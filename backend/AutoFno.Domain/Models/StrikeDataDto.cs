using AutoFno.Domain.Enums;

namespace AutoFno.Domain.Models;

public class OptionContractDetailDto
{
    public string InstrumentKey { get; set; } = string.Empty;
    public string TradingSymbol { get; set; } = string.Empty;
    public decimal Ltp { get; set; }
    public decimal PrevLtp { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public long OpenInterest { get; set; }
    public long PrevOpenInterest { get; set; }
    public long OiChange { get; set; }
    public decimal OiChangePercent { get; set; }
    public decimal BidPrice { get; set; }
    public int BidQty { get; set; }
    public decimal AskPrice { get; set; }
    public int AskQty { get; set; }
    public OptionGreeksDto Greeks { get; set; } = new();
    public BuildupType Buildup { get; set; } = BuildupType.Neutral;
}

public class StrikeDataDto
{
    public decimal StrikePrice { get; set; }
    public bool IsAtm { get; set; }
    public bool IsMaxPain { get; set; }
    public bool IsCallWall { get; set; }
    public bool IsPutWall { get; set; }
    public OptionContractDetailDto? Call { get; set; }
    public OptionContractDetailDto? Put { get; set; }
    public decimal StrikePcr { get; set; }
}
