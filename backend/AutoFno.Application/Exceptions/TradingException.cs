namespace AutoFno.Application.Exceptions;

public class TradingException : Exception
{
    public TradingException(string message) : base(message) { }
    public TradingException(string message, Exception innerException) : base(message, innerException) { }
}
