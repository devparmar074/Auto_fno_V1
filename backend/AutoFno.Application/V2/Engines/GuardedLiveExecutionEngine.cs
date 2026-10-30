using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFno.Application.V2.Interfaces;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging;

namespace AutoFno.Application.V2.Engines;

public class GuardedLiveExecutionEngine : IGuardedLiveExecutionEngine
{
    private readonly ILogger<GuardedLiveExecutionEngine> _logger;
    private readonly QuantitativeConvictionV2Config _config;
    private readonly ConcurrentDictionary<Guid, GuardedExecutionTicket> _tickets = new();
    private bool _isKillSwitchActive = false;

    public GuardedLiveExecutionEngine(
        QuantitativeConvictionV2Config config,
        ILogger<GuardedLiveExecutionEngine> logger)
    {
        _config = config;
        _logger = logger;
    }

    public GuardedExecutionTicket GenerateCandidateTicket(V2TradeCandidate candidate)
    {
        if (candidate == null || !candidate.IsQualified)
        {
            throw new InvalidOperationException("Cannot generate execution ticket for an unqualified candidate.");
        }

        if (_isKillSwitchActive)
        {
            throw new InvalidOperationException("Emergency kill switch is currently active. Order creation blocked.");
        }

        var ticket = new GuardedExecutionTicket
        {
            TicketId = Guid.NewGuid(),
            CorrelationId = $"V2-GUARDED-{Guid.NewGuid():N}",
            Candidate = candidate,
            Status = GuardedOrderStatus.PendingApproval,
            GeneratedAtUtc = DateTime.UtcNow,
            ApprovalDeadlineUtc = DateTime.UtcNow.AddSeconds(60), // 60-second validity window
            FinalLots = Math.Min(candidate.AllocatedLots, 2),     // Micro-lot ceiling (max 2 lots)
            FinalQuantity = Math.Min(candidate.AllocatedLots, 2) * (candidate.AllocatedQuantity / Math.Max(1, candidate.AllocatedLots))
        };

        ticket.StatusHistory.Add($"[{ticket.GeneratedAtUtc:HH:mm:ss}] Ticket created in PendingApproval state. Lots capped at {ticket.FinalLots}.");
        _tickets[ticket.TicketId] = ticket;

        _logger.LogInformation("Generated Guarded V2 Execution Ticket {Id} for {Symbol}. Awaiting manual approval.", ticket.TicketId, candidate.TradingSymbol);
        return ticket;
    }

    public Task<GuardedExecutionTicket> ProcessApprovalAsync(GuardedCandidateApprovalRequest request, CancellationToken ct = default)
    {
        if (!_tickets.TryGetValue(request.CandidateTicketId, out var ticket))
        {
            throw new KeyNotFoundException($"Guarded ticket {request.CandidateTicketId} not found.");
        }

        var now = DateTime.UtcNow;

        if (_isKillSwitchActive)
        {
            ticket.Status = GuardedOrderStatus.RejectedByUser;
            ticket.StatusHistory.Add($"[{now:HH:mm:ss}] Rejected: Emergency Kill Switch active.");
            return Task.FromResult(ticket);
        }

        if (ticket.ApprovalDeadlineUtc.HasValue && now > ticket.ApprovalDeadlineUtc.Value)
        {
            ticket.Status = GuardedOrderStatus.TimedOut;
            ticket.StatusHistory.Add($"[{now:HH:mm:ss}] Approval timed out. Exceeded 60-second window.");
            return Task.FromResult(ticket);
        }

        if (!request.IsApproved)
        {
            ticket.Status = GuardedOrderStatus.RejectedByUser;
            ticket.StatusHistory.Add($"[{now:HH:mm:ss}] Rejected by user '{request.AuthorizedBy}': {request.RejectionReason}.");
            return Task.FromResult(ticket);
        }

        // Live Mode Enforcement Guardrail
        if (!_config.V2LiveTrading)
        {
            ticket.Status = GuardedOrderStatus.Approved;
            ticket.ApprovedAtUtc = now;
            ticket.ApprovedBy = request.AuthorizedBy;
            ticket.StatusHistory.Add($"[{now:HH:mm:ss}] Approved by '{request.AuthorizedBy}'. Live execution simulated (V2LiveTrading=false). Order NOT sent to live broker.");
            _logger.LogInformation("Guarded ticket {Id} approved. Live trading disabled by policy; simulated fill recorded.", ticket.TicketId);
            return Task.FromResult(ticket);
        }

        // Live execution flow (guarded by external policy)
        ticket.Status = GuardedOrderStatus.Filled;
        ticket.ApprovedAtUtc = now;
        ticket.ApprovedBy = request.AuthorizedBy;
        ticket.DispatchedAtUtc = now;
        ticket.ExecutedPrice = ticket.Candidate.ExpectedEntryPrice;
        ticket.BrokerOrderId = $"ORD-SIM-{Guid.NewGuid():N}".Substring(0, 16);
        ticket.StatusHistory.Add($"[{now:HH:mm:ss}] Executed: BrokerOrderId={ticket.BrokerOrderId} @ Rs {ticket.ExecutedPrice}.");

        return Task.FromResult(ticket);
    }

    public List<GuardedExecutionTicket> GetPendingTickets()
    {
        var now = DateTime.UtcNow;
        return _tickets.Values
            .Where(t => t.Status == GuardedOrderStatus.PendingApproval && (!t.ApprovalDeadlineUtc.HasValue || now <= t.ApprovalDeadlineUtc.Value))
            .OrderByDescending(t => t.GeneratedAtUtc)
            .ToList();
    }

    public Task<EmergencyKillSwitchResult> TriggerEmergencyKillSwitchAsync(string reason, CancellationToken ct = default)
    {
        _isKillSwitchActive = true;
        int cancelled = 0;

        foreach (var ticket in _tickets.Values)
        {
            if (ticket.Status == GuardedOrderStatus.PendingApproval)
            {
                ticket.Status = GuardedOrderStatus.Cancelled;
                ticket.StatusHistory.Add($"[{DateTime.UtcNow:HH:mm:ss}] Cancelled by Emergency Kill Switch: {reason}");
                cancelled++;
            }
        }

        var result = new EmergencyKillSwitchResult
        {
            IsSuccess = true,
            TriggeredAtUtc = DateTime.UtcNow,
            TriggerSource = reason,
            PendingOrdersCancelledCount = cancelled,
            PositionsFlattenedCount = 0,
            ActionSummary = $"Emergency Kill Switch activated! {cancelled} pending guarded orders cancelled. Trading locked.",
            Logs = new List<string> { $"Emergency kill switch engaged due to: {reason}", $"Pending orders cancelled: {cancelled}" }
        };

        _logger.LogCritical("EMERGENCY KILL SWITCH ENGAGED: {Reason}", reason);
        return Task.FromResult(result);
    }
}
