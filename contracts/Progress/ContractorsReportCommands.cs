using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Every Contractor's Report on a project, newest period first.</summary>
public sealed record ListContractorsReports(string ProjectId) : IQuery<IReadOnlyList<ContractorsReport>>;

/// <summary>One report with the document it composes to right now.</summary>
public sealed record GetContractorsReport(string ContractorsReportId) : IQuery<ContractorsReportView?>;

/// <summary>
/// Opens a report for the week ending on a Thursday: the number defaults to one more than the
/// project's newest report, Look Ahead is seeded from the previous report's unstruck items,
/// Programme reference, Prepared by and Issued to carry forward, Valuation No. defaults to the
/// highest certificate on the register, Neighbours to its default line, and every progress
/// update in the period is selected. A second report for the same period is refused.
/// </summary>
public sealed record CreateContractorsReport(
    string ProjectId,
    DateOnly PeriodEnd,
    int? Number = null,
    string CreatedByEmail = "") : ICommand<ContractorsReport>;

/// <summary>Every entered field, written as posted — carry forward what should not change.</summary>
public sealed record UpdateContractorsReport(
    string ContractorsReportId,
    string ValuationNumber,
    string ProgrammeReference,
    string PreparedByName,
    string IssuedTo,
    DateOnly DateOfIssue,
    IReadOnlyList<ContractorsReportLookAheadItem> LookAhead,
    string Neighbours,
    string HealthAndSafety,
    string BuildingControlLiaison,
    IReadOnlyList<ContractorsReportAttendance> Attendance,
    IReadOnlyList<string> SelectedUpdateIds) : ICommand<ContractorsReport>;

public sealed record DeleteContractorsReport(string ContractorsReportId) : ICommand<Acknowledgement>;
