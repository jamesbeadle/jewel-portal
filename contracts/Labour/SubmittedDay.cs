using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>What the worker submitted with a timesheet's day, for the office to read before it
/// codes and approves the hours: the words of the day's note, its photographs, when they signed in
/// and out, and the records the log raised beside it. Null when the timesheet is not there.</summary>
public sealed record GetSubmittedDayForTimesheet(string TimesheetId) : IQuery<SubmittedDay?>;

public sealed record SubmittedDay(
    string TimesheetId,
    string WorkerName,
    DateTimeOffset WorkedOn,
    string ProjectId,
    string ProgressUpdateId,
    string Words,
    IReadOnlyList<SubmittedDayPhoto> Photos,
    DateTimeOffset? SignedInAt,
    DateTimeOffset? SignedOutAt,
    string SiteInstructionReference = "",
    string DefectReference = "",
    bool IsFiledLate = false)
{
    public bool HasNote => ProgressUpdateId.Length > 0;
    public int PhotoCount => Photos.Count;
    public bool HasPhotos => Photos.Count > 0;
    public bool HasSiteInstruction => SiteInstructionReference.Length > 0;
    public bool HasDefect => DefectReference.Length > 0;
}

public sealed record SubmittedDayPhoto(string ProgressPhotoId, string FileName);
