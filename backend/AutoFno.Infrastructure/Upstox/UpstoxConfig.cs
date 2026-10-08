namespace AutoFno.Infrastructure.Upstox;

public class UpstoxConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "https://localhost:5001/api/upstoxauth/callback";
    public string BaseUrl { get; set; } = "https://api.upstox.com/v2";
    public string AuthDialogUrl { get; set; } = "https://api.upstox.com/v2/login/authorization/dialog";
}
