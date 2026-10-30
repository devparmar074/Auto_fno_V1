using System;
using System.Collections.Generic;

namespace AutoFno.Domain.V2;

public class MaturityLevelStatusDto
{
    public MaturityLevel Level { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string ImplementationStatus { get; set; } = "IMPLEMENTED AND TESTED";
    public string OperationalStatus { get; set; } = "OPERATIONAL";
    public string TradingEvidenceStatus { get; set; } = "EVIDENCE ACTIVE";
    public string Description { get; set; } = string.Empty;
    public string VerificationEvidence { get; set; } = string.Empty;
    public int AutomatedTestsCount { get; set; }
    public DateTime CompletedDateUtc { get; set; }
    public string NextAdvancementRequirement { get; set; } = string.Empty;
}

public class SystemMaturityDashboardDto
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public MaturityLevel HighestFormallyVerifiedLevel { get; set; } = MaturityLevel.Level3_UnitAndIntegrationTesting;
    public MaturityLevel HighestActiveCapabilityLevel { get; set; } = MaturityLevel.Level5_ShadowPaperTrading;
    public int TotalAutomatedTestsPassing { get; set; }
    public int UnfinishedSoftwareTasksCount { get; set; } = 0;
    public List<MaturityLevelStatusDto> Levels { get; set; } = new();
    public string ArchitectureSummary { get; set; } = string.Empty;
}
