namespace Jewel.JPMS.Models;

public sealed record SiteReport(
    string SiteReportId,
    string ProjectId,
    DateTimeOffset PeriodEnd,
    string Narrative,
    int AttendanceDays,
    int OpenSnags,
    decimal ProgressPercent,
    bool IsIssued);

public sealed record ProgrammeTask(
    string ProgrammeTaskId,
    string ProjectId,
    string Title,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd,
    decimal ProgressPercent,
    string? BoqLineItemId);
