namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// The weekly Contractor's Report PLG receive (the FD's spec of 2026-09-15, change 4), as the
/// portal keeps it: ONE row per report holding what a person ENTERS — the header fields, Look
/// Ahead, Neighbours, H&amp;S, the Building Control liaison line, the subcontractors' attendance
/// and the updates chosen for Sections 1 and 9. Everything else the document prints (the
/// period's site notes, open RFIs, outstanding variations, the Building Control contact, the
/// work orders in the period) is READ from the register at build time and never carried
/// forward — see <see cref="ContractorsReportDocument"/>. The portal never emails it: Word and
/// PDF are downloaded and issued by a person.
/// </summary>
public sealed record ContractorsReport(
    string ContractorsReportId,
    string ProjectId,
    // "Report 29" — per project, entered on create (default: one more than the newest).
    int Number,
    // Friday to Thursday, named by the Thursday it ends on.
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    // Entered until interim certificates are filed to the register; the last certificate on the
    // register is shown beside it (ContractorsReportHeader.LastCertificateNumber).
    string ValuationNumber,
    string ProgrammeReference,
    string PreparedByName,
    string IssuedTo,
    DateOnly DateOfIssue,
    // Section 2, seeded from the previous report's unstruck items.
    IReadOnlyList<ContractorsReportLookAheadItem> LookAhead,
    // Sections 5, 6 and the liaison line of 7.
    string Neighbours,
    string HealthAndSafety,
    string BuildingControlLiaison,
    // Section 8's entered columns, per work order.
    IReadOnlyList<ContractorsReportAttendance> Attendance,
    // The progress updates in the period that Sections 1 and 9 print; all of them on create.
    IReadOnlyList<string> SelectedUpdateIds,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public string DisplayTitle => $"Contractor's Report No. {Number}";
}

/// <summary>A Look Ahead line; struck (IsDone) when the work happened, so the next report seeds
/// only what is still ahead.</summary>
public sealed record ContractorsReportLookAheadItem(string Text, bool IsDone);

/// <summary>Section 8's entered columns for one work order: days attended in the period, and
/// whether the client nominated the subcontractor.</summary>
public sealed record ContractorsReportAttendance(string WorkOrderId, int? AttendanceDays, bool IsClientNominated);

public static class ContractorsReportDefaults
{
    public const string Neighbours = "No issues have been reported.";
}
