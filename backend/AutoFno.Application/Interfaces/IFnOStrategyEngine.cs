using AutoFno.Domain.Models;

namespace AutoFno.Application.Interfaces;

public interface IFnOStrategyEngine
{
    Task<FnOStrategyScoreDto> EvaluateSetupAsync(
        OptionChainDto optionChain,
        PcrSummaryDto pcrSummary,
        MaxPainDto maxPain,
        List<Candle> spotCandles,
        CancellationToken ct = default);
}
