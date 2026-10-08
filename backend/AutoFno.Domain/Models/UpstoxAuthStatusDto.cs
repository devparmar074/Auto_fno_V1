namespace AutoFno.Domain.Models;

public class UpstoxAuthStatusDto
{
    public bool IsConnected { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PrimaryIp { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string Broker { get; set; } = "UPSTOX";
}
