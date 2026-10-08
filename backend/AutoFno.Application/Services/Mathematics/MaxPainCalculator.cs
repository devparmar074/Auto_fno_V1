using AutoFno.Domain.Models;

namespace AutoFno.Application.Services.Mathematics;

public static class MaxPainCalculator
{
    /// <summary>
    /// Calculates the Max Pain strike where option buyers lose the most money (option writers maximize profit).
    /// </summary>
    public static MaxPainDto CalculateMaxPain(List<StrikeDataDto> strikes, decimal spotPrice, string expiryDate = "")
    {
        if (strikes == null || strikes.Count == 0)
        {
            return new MaxPainDto
            {
                MaxPainStrike = spotPrice,
                SpotPrice = spotPrice,
                ExpiryDate = expiryDate
            };
        }

        var candidateStrikes = strikes.Select(s => s.StrikePrice).Distinct().OrderBy(s => s).ToList();

        decimal minTotalLoss = decimal.MaxValue;
        decimal maxPainStrike = candidateStrikes[0];

        foreach (var expiryTarget in candidateStrikes)
        {
            decimal totalLoss = 0m;

            foreach (var strike in strikes)
            {
                // Call Loss: If target > strike, Call is ITM and buyers collect (target - strike) * CallOI
                if (strike.Call != null && strike.Call.OpenInterest > 0)
                {
                    if (expiryTarget > strike.StrikePrice)
                    {
                        totalLoss += (expiryTarget - strike.StrikePrice) * strike.Call.OpenInterest;
                    }
                }

                // Put Loss: If target < strike, Put is ITM and buyers collect (strike - target) * PutOI
                if (strike.Put != null && strike.Put.OpenInterest > 0)
                {
                    if (expiryTarget < strike.StrikePrice)
                    {
                        totalLoss += (strike.StrikePrice - expiryTarget) * strike.Put.OpenInterest;
                    }
                }
            }

            if (totalLoss < minTotalLoss)
            {
                minTotalLoss = totalLoss;
                maxPainStrike = expiryTarget;
            }
        }

        // Mark the MaxPain strike in the strikes list
        foreach (var s in strikes)
        {
            s.IsMaxPain = (s.StrikePrice == maxPainStrike);
        }

        var diff = maxPainStrike - spotPrice;
        var diffPercent = spotPrice > 0 ? Math.Round((diff / spotPrice) * 100m, 2) : 0m;

        return new MaxPainDto
        {
            MaxPainStrike = maxPainStrike,
            TotalPainValue = Math.Round(minTotalLoss / 10000000m, 2), // Expressed in Crores or scaled
            SpotPrice = spotPrice,
            DistanceFromSpot = Math.Round(diff, 2),
            DistancePercent = diffPercent,
            ExpiryDate = expiryDate,
            CalculatedAtUtc = DateTime.UtcNow
        };
    }
}
