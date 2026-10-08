using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AutoFno.Infrastructure.Hubs;

public class TradingHub : Hub
{
    private readonly ILogger<TradingHub> _logger;

    public TradingHub(ILogger<TradingHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
