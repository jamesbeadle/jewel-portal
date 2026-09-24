using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Every Contractor's Report on a project, newest period first.</summary>
public sealed record ListContractorsReports(string ProjectId) : IQuery<IReadOnlyList<ContractorsReport>>;

/// <summary>One report with the document it composes to right now.</summary>
public sealed record GetContractorsReport(string ContractorsReportId) : IQuery<ContractorsReportView?>;

/// <summary>
/// Opens a report for the week ending on a Thursday: the number defaults to one more than the
/// project's newest report, Look Ahead is seeded from the previous report's unstruck items,
/// Programme reference, Prepared by, Issued to, H&amp;S, the Building Control contact and liaison
/// carry forward, Valuation No. is the project's current valuation (the newest not yet
/// Confirmed), the date of issue is the Friday after the period, Neighbours takes its default
/// line, and every progress update in the period is selected. A second report for the same
/// period is refused.
/// </summary>
public sealed record CreateContractorsReport(
    string ProjectId,
    DateOnly PeriodEnd,
    int? Number = null,
    string CreatedByEmail = "") : ICommand<ContractorsReport>;

/// <summary>Every entered field, written as posted — carry forward what should not change. The two
/// fields added on 24 Sep 2026, <paramref name="BuildingControlContact"/> and
/// <paramref name="ExcludedPhotoIds"/>, keep their stored value when not supplied (null).</summary>
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
    IReadOnlyList<string> SelectedUpdateIds,
    string? BuildingControlContact = null,
    IReadOnlyList<string>? ExcludedPhotoIds = null) : ICommand<ContractorsReport>;

public sealed record DeleteContractorsReport(string ContractorsReportId) : ICommand<Acknowledgement>;
