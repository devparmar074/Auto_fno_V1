namespace AutoFno.Domain.Entities;

public class UpstoxConnection
{
    public int Id { get; set; }
    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string? EncryptedRefreshToken { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PrimaryIp { get; set; }
    public string Broker { get; set; } = "UPSTOX";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
