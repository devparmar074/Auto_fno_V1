using AutoFno.Application.Interfaces;
using AutoFno.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoFno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UpstoxAuthController : ControllerBase
{
    private readonly IUpstoxAuthService _authService;

    public UpstoxAuthController(IUpstoxAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("login")]
    public IActionResult GetLoginUrl([FromQuery] string? state = null)
    {
        var url = _authService.GetAuthorizationUrl(state);
        return Ok(new { authorizationUrl = url });
    }

    [HttpGet("callback")]
    public async Task<IActionResult> HandleCallback([FromQuery] string code, CancellationToken ct)
    {
        var status = await _authService.CompleteOAuthAsync(code, ct);
        // Redirect back to frontend
        return Redirect("http://localhost:5174/?auth_success=true");
    }

    public record TokenRequest(string AccessToken, string? RefreshToken = null);

    [HttpPost("token")]
    public async Task<ActionResult<UpstoxAuthStatusDto>> SetManualToken([FromBody] TokenRequest req, CancellationToken ct)
    {
        var status = await _authService.SetManualTokenAsync(req.AccessToken, req.RefreshToken, ct);
        return Ok(status);
    }

    [HttpGet("status")]
    public async Task<ActionResult<UpstoxAuthStatusDto>> GetStatus(CancellationToken ct)
    {
        var status = await _authService.GetStatusAsync(ct);
        return Ok(status);
    }

    [HttpGet("ip")]
    public async Task<IActionResult> GetRegisteredIp(CancellationToken ct)
    {
        var ip = await _authService.GetRegisteredIpAsync(ct);
        return Ok(new { registeredIp = ip });
    }

    public record IpUpdateRequest(string PrimaryIp, string? SecondaryIp = null);

    [HttpPost("ip")]
    public async Task<IActionResult> SetRegisteredIp([FromBody] IpUpdateRequest req, CancellationToken ct)
    {
        var result = await _authService.SetRegisteredIpAsync(req.PrimaryIp, req.SecondaryIp, ct);
        return Ok(new { result });
    }
}
