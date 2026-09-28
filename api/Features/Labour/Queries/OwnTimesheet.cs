namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>One of the worker's own timesheets as the day reads it back to them — hours and
/// status only, never a rate.</summary>
public sealed record OwnTimesheet(
    string TimesheetId,
    string ProjectId,
    string ProjectName,
    DateTimeOffset WorkedOn,
    decimal Hours,
    string CostCode,
    int Status,
    string RejectionReason);
