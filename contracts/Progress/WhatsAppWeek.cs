namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// A week of the site WhatsApp group, read for one project before anything is written: the days
/// it found, what each day would become, and everything a person must look at first. The
/// portal cuts the export Friday to the following Thursday, keeps the messages whose sender is
/// on this project's site-note sender list, sets aside the senders on another project's list,
/// and lists the rest for review rather than guessing.
/// </summary>
public sealed record WhatsAppWeekPreview(
    string ProjectId,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    IReadOnlyList<WhatsAppWeekDay> Days,
    IReadOnlyList<WhatsAppUnattributedMessage> Unattributed,
    IReadOnlyList<WhatsAppRepeatedPhoto> RepeatedPhotos,
    int OtherSiteMessageCount,
    IReadOnlyList<string> OtherSiteSenders,
    int MessagesOutsideWeek,
    IReadOnlyList<string> MissingMediaFileNames,
    IReadOnlyList<string> Notes);

/// <summary>One day of the week that would become one progress update.</summary>
public sealed record WhatsAppWeekDay(
    DateOnly Date,
    string Title,
    string Description,
    int MessageCount,
    IReadOnlyList<string> PhotoFileNames,
    IReadOnlyList<string> ExistingUpdateTitles)
{
    public bool HasExistingUpdate => ExistingUpdateTitles.Count > 0;
}

/// <summary>A message whose sender is on no project's sender list — a person decides.</summary>
public sealed record WhatsAppUnattributedMessage(
    DateOnly Date,
    string Time,
    string Sender,
    string Text,
    int PhotoCount);

/// <summary>A photograph the export carried on more than one day: kept on the first, dropped
/// from the rest, and said so.</summary>
public sealed record WhatsAppRepeatedPhoto(
    string FileName,
    DateOnly KeptOn,
    IReadOnlyList<DateOnly> DroppedFrom);

/// <summary>What the apply wrote: one line per day, with the photo outcomes behind it.</summary>
public sealed record WhatsAppWeekApplied(
    IReadOnlyList<WhatsAppWeekDayWritten> Days);

public sealed record WhatsAppWeekDayWritten(
    DateOnly Date,
    string ProgressUpdateId,
    string Title,
    IReadOnlyList<ProgressPhotoIntakeOutcome> Photos);
