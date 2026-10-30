using System;
using System.Collections.Generic;
using AutoFno.Domain.Enums;

namespace AutoFno.Domain.V2;

public class GuardedCandidateApprovalRequest
{
    public Guid CandidateTicketId { get; set; }
    public bool IsApproved { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public string AuthorizedBy { get; set; } = "Trader";
    public int ApprovedLots { get; set; } = 1;
}

public class GuardedExecutionTicket
{
    public Guid TicketId { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public V2TradeCandidate Candidate { get; set; } = new();
    public GuardedOrderStatus Status { get; set; } = GuardedOrderStatus.PendingApproval;

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovalDeadlineUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public string? ApprovedBy { get; set; }
    public int FinalLots { get; set; }
    public int FinalQuantity { get; set; }
    public decimal ExecutedPrice { get; set; }
    public string? BrokerOrderId { get; set; }
    public string? ExecutionAuditLog { get; set; }
    public List<string> StatusHistory { get; set; } = new();
}

public class BrokerReconciliationReport
{
    public DateTime ReconciledAtUtc { get; set; } = DateTime.UtcNow;
    public int InternalActivePositionsCount { get; set; }
    public int BrokerActivePositionsCount { get; set; }
    public bool IsInSync { get; set; }
    public List<string> Discrepancies { get; set; } = new();
    public List<string> CorrectiveActions { get; set; } = new();
}

public class AutomatedExecutionStatusDto
{
    public AutomatedExecutionState CurrentState { get; set; } = AutomatedExecutionState.Standby;
    public bool IsLiveTradingEnabled { get; set; } = false;
    public bool IsAutomatedExecutionAuthorized { get; set; } = false;
    public bool IsCircuitBreakerTripped { get; set; } = false;
    public bool IsEmergencyKillSwitchActive { get; set; } = false;

    public int TodaySignalsEvaluated { get; set; }
    public int TodayTradesPlaced { get; set; }
    public decimal TodayRealizedPnl { get; set; }
    public int ConsecutiveLossesCount { get; set; }
    public decimal PeakDailyLossReached { get; set; }

    public V2HypotheticalPosition? ActivePosition { get; set; }
    public DateTime LastEvaluationTimeUtc { get; set; }
    public string StateDescription { get; set; } = "Automated execution engine is standby in guarded passive mode.";
}

public class EmergencyKillSwitchResult
{
    public bool IsSuccess { get; set; }
    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public string TriggerSource { get; set; } = string.Empty;
    public int PositionsFlattenedCount { get; set; }
    public int PendingOrdersCancelledCount { get; set; }
    public string ActionSummary { get; set; } = string.Empty;
    public List<string> Logs { get; set; } = new();
}
