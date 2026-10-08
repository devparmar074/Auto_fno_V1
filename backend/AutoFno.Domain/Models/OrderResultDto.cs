using AutoFno.Domain.Enums;

namespace AutoFno.Domain.Models;

public class OrderResultDto
{
    public bool Success { get; set; }
    public string? OrderId { get; set; }
    public string? CorrelationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal ExecutedPrice { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
