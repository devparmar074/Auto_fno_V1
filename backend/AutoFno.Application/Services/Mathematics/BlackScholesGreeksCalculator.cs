using AutoFno.Domain.Enums;
using AutoFno.Domain.Models;

namespace AutoFno.Application.Services.Mathematics;

public static class BlackScholesGreeksCalculator
{
    private const double RiskFreeRate = 0.07; // 7% annualized risk-free rate for Indian Equities / RBI Repo

    /// <summary>
    /// Computes full European Option Greeks (Delta, Gamma, Theta, Vega, Rho) and IV.
    /// </summary>
    /// <param name="spot">Underlying Spot Price (e.g. NIFTY 25000)</param>
    /// <param name="strike">Option Strike Price (e.g. 25000)</param>
    /// <param name="daysToExpiry">Calendar days to expiry (fractional allowed)</param>
    /// <param name="optionType">CE or PE</param>
    /// <param name="marketPrice">Current Market LTP of option</param>
    /// <param name="providedIv">Optional broker provided IV (if 0 or negative, solver will compute)</param>
    /// <param name="r">Annualized risk-free rate (default 0.07)</param>
    public static OptionGreeksDto CalculateGreeks(
        decimal spot,
        decimal strike,
        double daysToExpiry,
        OptionType optionType,
        decimal marketPrice,
        decimal providedIv = 0,
        double r = RiskFreeRate)
    {
        var S = (double)spot;
        var K = (double)strike;
        var price = (double)marketPrice;
        var T = Math.Max(daysToExpiry / 365.0, 0.0001); // Avoid division by zero

        if (S <= 0 || K <= 0)
        {
            return new OptionGreeksDto();
        }

        // Determine Implied Volatility
        double sigma;
        if (providedIv > 0 && providedIv < 500)
        {
            sigma = (double)providedIv / 100.0;
        }
        else
        {
            sigma = SolveImpliedVolatility(S, K, T, price, optionType, r);
        }

        var d1 = (Math.Log(S / K) + (r + (sigma * sigma / 2.0)) * T) / (sigma * Math.Sqrt(T));
        var d2 = d1 - sigma * Math.Sqrt(T);

        var cdfD1 = NormalCdf(d1);
        var cdfD2 = NormalCdf(d2);
        var pdfD1 = NormalPdf(d1);

        double delta;
        double theta;
        double rho;

        if (optionType == OptionType.CE)
        {
            delta = cdfD1;
            // Theta per calendar day
            theta = (-(S * pdfD1 * sigma) / (2.0 * Math.Sqrt(T)) - r * K * Math.Exp(-r * T) * cdfD2) / 365.0;
            // Rho per 1% change in rate
            rho = (K * T * Math.Exp(-r * T) * cdfD2) / 100.0;
        }
        else
        {
            delta = cdfD1 - 1.0;
            // Theta per calendar day
            var cdfNegD2 = NormalCdf(-d2);
            theta = (-(S * pdfD1 * sigma) / (2.0 * Math.Sqrt(T)) + r * K * Math.Exp(-r * T) * cdfNegD2) / 365.0;
            // Rho per 1% change in rate
            rho = (-K * T * Math.Exp(-r * T) * cdfNegD2) / 100.0;
        }

        // Gamma (identical for Call and Put)
        var gamma = pdfD1 / (S * sigma * Math.Sqrt(T));

        // Vega per 1% change in volatility (identical for Call and Put)
        var vega = (S * pdfD1 * Math.Sqrt(T)) / 100.0;

        // Approximate Probability of Profit (POP)
        var pop = optionType == OptionType.CE ? (1.0 - NormalCdf(d2)) * 100.0 : NormalCdf(-d2) * 100.0;

        return new OptionGreeksDto
        {
            Delta = Math.Round((decimal)delta, 4),
            Gamma = Math.Round((decimal)gamma, 6),
            Theta = Math.Round((decimal)theta, 2),
            Vega = Math.Round((decimal)vega, 2),
            Rho = Math.Round((decimal)rho, 4),
            Iv = Math.Round((decimal)(sigma * 100.0), 2),
            Pop = Math.Round((decimal)Math.Clamp(pop, 0.0, 100.0), 1)
        };
    }

    /// <summary>
    /// Theoretical Black-Scholes Option Pricing.
    /// </summary>
    public static double BlackScholesPrice(double S, double K, double T, double sigma, OptionType optionType, double r = RiskFreeRate)
    {
        if (T <= 0 || sigma <= 0)
        {
            return optionType == OptionType.CE ? Math.Max(0.0, S - K) : Math.Max(0.0, K - S);
        }

        var d1 = (Math.Log(S / K) + (r + (sigma * sigma / 2.0)) * T) / (sigma * Math.Sqrt(T));
        var d2 = d1 - sigma * Math.Sqrt(T);

        if (optionType == OptionType.CE)
        {
            return S * NormalCdf(d1) - K * Math.Exp(-r * T) * NormalCdf(d2);
        }
        else
        {
            return K * Math.Exp(-r * T) * NormalCdf(-d2) - S * NormalCdf(-d1);
        }
    }

    /// <summary>
    /// Solves for Implied Volatility using Newton-Raphson with Bisection fallback.
    /// </summary>
    public static double SolveImpliedVolatility(double S, double K, double T, double marketPrice, OptionType optionType, double r = RiskFreeRate)
    {
        // Check intrinsic value
        var intrinsic = optionType == OptionType.CE ? Math.Max(0.0, S - K) : Math.Max(0.0, K - S);
        if (marketPrice <= intrinsic)
        {
            return 0.15; // default 15%
        }

        double sigma = 0.20; // 20% starting guess
        const int maxIterations = 60;
        const double tolerance = 0.001;

        for (int i = 0; i < maxIterations; i++)
        {
            var price = BlackScholesPrice(S, K, T, sigma, optionType, r);
            var diff = price - marketPrice;

            if (Math.Abs(diff) < tolerance)
            {
                return sigma;
            }

            var d1 = (Math.Log(S / K) + (r + (sigma * sigma / 2.0)) * T) / (sigma * Math.Sqrt(T));
            var vega = S * NormalPdf(d1) * Math.Sqrt(T);

            if (vega < 1e-6)
            {
                break; // Switch to bisection
            }

            var nextSigma = sigma - diff / vega;
            if (nextSigma <= 0.01 || nextSigma > 4.0)
            {
                break; // Out of bounds, switch to bisection
            }
            sigma = nextSigma;
        }

        // Bisection fallback
        double low = 0.01;
        double high = 3.50;
        for (int i = 0; i < 50; i++)
        {
            var mid = (low + high) / 2.0;
            var price = BlackScholesPrice(S, K, T, mid, optionType, r);
            var diff = price - marketPrice;

            if (Math.Abs(diff) < tolerance)
            {
                return mid;
            }

            if (diff > 0)
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
        }

        return (low + high) / 2.0;
    }

    /// <summary>
    /// Cumulative distribution function for standard normal distribution using Hastings approximation.
    /// </summary>
    public static double NormalCdf(double x)
    {
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        int sign = 1;
        if (x < 0)
        {
            sign = -1;
            x = -x;
        }

        double t = 1.0 / (1.0 + p * x);
        double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x / 2.0);

        return 0.5 * (1.0 + sign * y);
    }

    /// <summary>
    /// Probability density function for standard normal distribution.
    /// </summary>
    public static double NormalPdf(double x)
    {
        return (1.0 / Math.Sqrt(2.0 * Math.PI)) * Math.Exp(-0.5 * x * x);
    }
}
