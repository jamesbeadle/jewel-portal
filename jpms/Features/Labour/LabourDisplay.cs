namespace Jewel.JPMS.Features.Labour;

/// <summary>How labour records read on screen — status and absence words shared by the
/// overview page and its worker components, defined once.</summary>
public static class LabourDisplay
{
    public static string StatusLabel(TimesheetStatus status) => status switch
    {
        TimesheetStatus.Approved => "approved",
        TimesheetStatus.Rejected => "sent back",
        _ => "waiting",
    };

    public static string AbsenceLabel(AbsenceKind kind) => kind switch
    {
        AbsenceKind.Holiday => "Holiday",
        AbsenceKind.HalfDay => "Half day",
        AbsenceKind.NotWorked => "Not worked",
        _ => "Sick",
    };
}
