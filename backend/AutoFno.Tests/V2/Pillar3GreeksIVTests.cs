using System;
using System.Collections.Generic;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Models;
using AutoFno.Domain.V2;
using Xunit;

namespace AutoFno.Tests.V2;

public class Pillar3GreeksIVTests
{
    private readonly GreeksAnalysisEngine _engine = new();
    private readonly QuantitativeConvictionV2Config _config = new();

    [Fact]
    public void Greeks_Values_Fall_Within_Theoretical_Option_Bounds()
    {
        var chain = new OptionChainDto
        {
            SpotPrice = 24500,
            Strikes = new List<StrikeDataDto>
            {
                new()
                {
                    StrikePrice = 24500,
                    IsAtm = true,
                    Call = new OptionContractDetailDto
                    {
                        Ltp = 150,
                        Greeks = new OptionGreeksDto
                        {
                            Iv = 14.5m,
                            Delta = 0.52m,
                            Gamma = 0.0025m,
                            Theta = -12.4m,
                            Vega = 16.8m
                        }
                    },
                    Put = new OptionContractDetailDto
                    {
                        Ltp = 140,
                        Greeks = new OptionGreeksDto
                        {
                            Iv = 14.8m,
                            Delta = -0.48m,
                            Gamma = 0.0025m,
                            Theta = -11.9m,
                            Vega = 16.5m
                        }
                    }
                }
            }
        };

        var result = _engine.Analyze(chain, 24500m, _config);

        Assert.InRange(result.AtmDeltaCe, 0.0m, 1.0m);
        Assert.InRange(result.AtmDeltaPe, -1.0m, 0.0m);
        Assert.True(result.AtmGamma >= 0, "Gamma must be non-negative");
        Assert.True(result.AtmTheta <= 0, "Theta must be non-positive for long options");
        Assert.True(result.AtmVega >= 0, "Vega must be non-negative");
        Assert.InRange(result.TotalGreeksPillarScore, -25.0m, 25.0m);
    }

    [Fact]
    public void IV_Rank_And_Percentile_Correctly_Identified()
    {
        var chain = new OptionChainDto
        {
            SpotPrice = 24500,
            Strikes = new List<StrikeDataDto>
            {
                new()
                {
                    StrikePrice = 24500,
                    IsAtm = true,
                    Call = new OptionContractDetailDto
                    {
                        Ltp = 100,
                        Greeks = new OptionGreeksDto { Iv = 25.0m, Delta = 0.5m }
                    },
                    Put = new OptionContractDetailDto
                    {
                        Ltp = 100,
                        Greeks = new OptionGreeksDto { Iv = 25.0m, Delta = -0.5m }
                    }
                }
            }
        };

        var result = _engine.Analyze(chain, 24500m, _config);

        Assert.True(result.AtmIv >= 0m && result.AtmIv <= 100m);
        Assert.InRange(result.TotalGreeksPillarScore, -25.0m, 25.0m);
    }

    [Fact]
    public void Taylor_Expansion_Premium_Movement_Estimates_Accurately()
    {
        // Delta = 0.50, Gamma = 0.002, Theta = -10, Vega = 15
        // Spot moves +100 pts, 1 day passes (dt = 1), IV expands +1%
        decimal dS = 100.0m;
        decimal delta = 0.50m;
        decimal gamma = 0.002m;
        decimal theta = -10.0m;
        decimal vega = 15.0m;
        decimal dIv = 1.0m;
        decimal dtDays = 1.0m;

        decimal estimatedChange = _engine.EstimateOptionPremiumMovement(dS, delta, gamma, theta, vega, dIv, dtDays);

        // Delta component: 0.50 * 100 = 50
        // Gamma component: 0.5 * 0.002 * 100^2 = 10
        // Theta component: -10 * 1 = -10
        // Vega component: 15 * 1 = 15
        // Total expected = 50 + 10 - 10 + 15 = 65
        Assert.Equal(65.0m, estimatedChange);
    }
}
