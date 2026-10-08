namespace AutoFno.Domain.Models;

public class OptionGreeksDto
{
    public decimal Delta { get; set; }
    public decimal Gamma { get; set; }
    public decimal Theta { get; set; }
    public decimal Vega { get; set; }
    public decimal Rho { get; set; }
    public decimal Iv { get; set; } // Implied Volatility % (e.g. 14.5%)
    public decimal Pop { get; set; } // Probability of Profit %
}
