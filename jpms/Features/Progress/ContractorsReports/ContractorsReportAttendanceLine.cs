using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.ContractorsReports;

/// <summary>One work order's Section 8 line as the editor holds it: the days ticked on site, the
/// week's scope and the client-nominated flag.</summary>
public sealed class ContractorsReportAttendanceLine
{
    public int? AttendanceDays { get; set; }
    public bool IsClientNominated { get; set; }
    public string Scope { get; set; } = "";
    public HashSet<DateOnly> DaysOnSite { get; } = new();

    public bool HasAnything =>
        AttendanceDays is not null || IsClientNominated || !string.IsNullOrWhiteSpace(Scope) || DaysOnSite.Count > 0;

    public void SetOnSite(DateOnly day, bool isOnSite)
    {
        if (isOnSite) { DaysOnSite.Add(day); return; }
        DaysOnSite.Remove(day);
    }

    public static ContractorsReportAttendanceLine From(ContractorsReportAttendance attendance)
    {
        var line = new ContractorsReportAttendanceLine
        {
            AttendanceDays = attendance.AttendanceDays, IsClientNominated = attendance.IsClientNominated, Scope = attendance.Scope
        };
        line.DaysOnSite.UnionWith(attendance.DaysOnSite ?? Array.Empty<DateOnly>());
        return line;
    }

    /// <summary>The days ticked are the count; with none ticked, a count entered before the ticks existed stands.</summary>
    public ContractorsReportAttendance ToAttendance(string workOrderId)
    {
        var days = DaysOnSite.Order().ToList();
        var count = days.Count > 0 ? days.Count : AttendanceDays;
        return new ContractorsReportAttendance(workOrderId, count, IsClientNominated, Scope.Trim(), days);
    }
}
