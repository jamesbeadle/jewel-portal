using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.ContractorsReports;

/// <summary>The entered half of a Contractor's Report as the editor holds it between saves —
/// every field the record stores, mutable for binding, and nothing the register composes.</summary>
public sealed class ContractorsReportDraft
{
    public string ValuationNumber { get; set; } = "";
    public string ProgrammeReference { get; set; } = "";
    public string PreparedByName { get; set; } = "";
    public string IssuedTo { get; set; } = "";
    public DateOnly DateOfIssue { get; set; }
    public List<ContractorsReportLookAheadLine> LookAhead { get; } = new();
    public string Neighbours { get; set; } = "";
    public string HealthAndSafety { get; set; } = "";
    public string BuildingControlLiaison { get; set; } = "";
    public Dictionary<string, ContractorsReportAttendanceLine> Attendance { get; } = new();
    public HashSet<string> SelectedUpdateIds { get; } = new();
    public string BuildingControlContact { get; set; } = "";
    public HashSet<string> ExcludedPhotoIds { get; } = new();

    public static ContractorsReportDraft From(ContractorsReport report)
    {
        var draft = new ContractorsReportDraft
        {
            ValuationNumber = report.ValuationNumber,
            ProgrammeReference = report.ProgrammeReference,
            PreparedByName = report.PreparedByName,
            IssuedTo = report.IssuedTo,
            DateOfIssue = report.DateOfIssue,
            Neighbours = report.Neighbours,
            HealthAndSafety = report.HealthAndSafety,
            BuildingControlLiaison = report.BuildingControlLiaison,
            BuildingControlContact = report.BuildingControlContact
        };
        draft.LookAhead.AddRange(report.LookAhead.Select(item => new ContractorsReportLookAheadLine { Text = item.Text, IsDone = item.IsDone }));
        foreach (var item in report.Attendance)
            draft.Attendance[item.WorkOrderId] = new ContractorsReportAttendanceLine { AttendanceDays = item.AttendanceDays, IsClientNominated = item.IsClientNominated, Scope = item.Scope };
        draft.SelectedUpdateIds.UnionWith(report.SelectedUpdateIds);
        draft.ExcludedPhotoIds.UnionWith(report.ExcludedPhotoIds);
        return draft;
    }

    public UpdateContractorsReport ToCommand(string contractorsReportId) => new(
        contractorsReportId,
        ValuationNumber,
        ProgrammeReference,
        PreparedByName,
        IssuedTo,
        DateOfIssue,
        LookAhead.Select(line => new ContractorsReportLookAheadItem(line.Text, line.IsDone)).ToList(),
        Neighbours,
        HealthAndSafety,
        BuildingControlLiaison,
        Attendance
            .Where(pair => pair.Value.HasAnything)
            .Select(pair => new ContractorsReportAttendance(pair.Key, pair.Value.AttendanceDays, pair.Value.IsClientNominated, pair.Value.Scope.Trim()))
            .ToList(),
        SelectedUpdateIds.ToList(),
        BuildingControlContact,
        ExcludedPhotoIds.ToList());

    public ContractorsReportAttendanceLine AttendanceFor(string workOrderId)
    {
        if (!Attendance.TryGetValue(workOrderId, out var line)) Attendance[workOrderId] = line = new ContractorsReportAttendanceLine();
        return line;
    }
}

public sealed class ContractorsReportLookAheadLine
{
    public string Text { get; set; } = "";
    public bool IsDone { get; set; }
}

public sealed class ContractorsReportAttendanceLine
{
    public int? AttendanceDays { get; set; }
    public bool IsClientNominated { get; set; }
    public string Scope { get; set; } = "";

    public bool HasAnything => AttendanceDays is not null || IsClientNominated || !string.IsNullOrWhiteSpace(Scope);
}
