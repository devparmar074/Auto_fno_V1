using System;
using System.Threading.Tasks;
using AutoFno.Application.V2.Engines;
using AutoFno.Domain.Enums;
using AutoFno.Domain.V2;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoFno.Tests.V2;

public class GuardedLiveExecutionTests
{
    private readonly GuardedLiveExecutionEngine _engine;
    private readonly QuantitativeConvictionV2Config _config;

    public GuardedLiveExecutionTests()
    {
        _config = new QuantitativeConvictionV2Config
        {
            EnableV2 = true,
            V2LiveTrading = false // Safely disabled
        };
        _engine = new GuardedLiveExecutionEngine(_config, new NullLogger<GuardedLiveExecutionEngine>());
    }

    [Fact]
    public void GenerateCandidateTicket_ShouldEnforceMicroLotCeiling()
    {
        var candidate = new V2TradeCandidate
        {
            IsQualified = true,
            TradingSymbol = "NIFTY 24500 CE",
            AllocatedLots = 10, // Attempt 10 lots
            AllocatedQuantity = 750,
            ExpectedEntryPrice = 150m
        };

        var ticket = _engine.GenerateCandidateTicket(candidate);

        Assert.Equal(GuardedOrderStatus.PendingApproval, ticket.Status);
        Assert.Equal(2, ticket.FinalLots); // Capped at 2 lots
        Assert.Equal(150, ticket.FinalQuantity);
        Assert.NotNull(ticket.ApprovalDeadlineUtc);
    }

    [Fact]
    public async Task ProcessApproval_WhenApproved_ShouldUpdateStatusSafely()
    {
        var candidate = new V2TradeCandidate
        {
            IsQualified = true,
            TradingSymbol = "NIFTY 24500 CE",
            AllocatedLots = 1,
            AllocatedQuantity = 75,
            ExpectedEntryPrice = 120m
        };

        var ticket = _engine.GenerateCandidateTicket(candidate);

        var approvalRequest = new GuardedCandidateApprovalRequest
        {
            CandidateTicketId = ticket.TicketId,
            IsApproved = true,
            AuthorizedBy = "SeniorTrader"
        };

        var updated = await _engine.ProcessApprovalAsync(approvalRequest);

        Assert.Equal(GuardedOrderStatus.Approved, updated.Status);
        Assert.Equal("SeniorTrader", updated.ApprovedBy);
        Assert.NotNull(updated.ApprovedAtUtc);
    }

    [Fact]
    public async Task ProcessApproval_WhenRejected_ShouldRecordUserRejection()
    {
        var candidate = new V2TradeCandidate
        {
            IsQualified = true,
            TradingSymbol = "NIFTY 24500 PE",
            AllocatedLots = 1,
            AllocatedQuantity = 75
        };

        var ticket = _engine.GenerateCandidateTicket(candidate);

        var rejectionRequest = new GuardedCandidateApprovalRequest
        {
            CandidateTicketId = ticket.TicketId,
            IsApproved = false,
            RejectionReason = "RBI policy rate decision imminent.",
            AuthorizedBy = "RiskManager"
        };

        var updated = await _engine.ProcessApprovalAsync(rejectionRequest);

        Assert.Equal(GuardedOrderStatus.RejectedByUser, updated.Status);
        Assert.Contains(updated.StatusHistory, s => s.Contains("RBI policy rate decision"));
    }

    [Fact]
    public async Task TriggerEmergencyKillSwitch_ShouldCancelPendingTicketsAndLockTrading()
    {
        var candidate = new V2TradeCandidate
        {
            IsQualified = true,
            TradingSymbol = "NIFTY 24500 CE",
            AllocatedLots = 1,
            AllocatedQuantity = 75
        };

        var ticket = _engine.GenerateCandidateTicket(candidate);

        var killResult = await _engine.TriggerEmergencyKillSwitchAsync("Sudden market crash detected");

        Assert.True(killResult.IsSuccess);
        Assert.Equal(1, killResult.PendingOrdersCancelledCount);

        // Verify subsequent order creation throws exception
        Assert.Throws<InvalidOperationException>(() => _engine.GenerateCandidateTicket(candidate));
    }
}
