namespace Jewel.JPMS.Models;

/// <summary>
/// Why the weekly-report run left a pool photograph out of the report (2026-09-24, Nigel, for
/// Jeremy's run: "identify images that are not going to be in the weekly report … archive the
/// images as images that were dumped for the week but we will keep them in archive just in
/// case"). The criteria are the jpms-contractors-report skill's; this is their vocabulary.
/// </summary>
public enum SitePhotoArchiveReason
{
    NotProgress = 0,
    NoValue = 1,
    DocumentOrDrawing = 2,
    Screenshot = 3,
    DrawnOver = 4,
    SnagOrDefect = 5,
    Other = 6
}

/// <summary>A photograph set aside from the report: the reason, the sentence saying why, and the
/// project and Friday-to-Thursday week whose dump it came in (either may be unknown).</summary>
public sealed record SitePhotoArchive(
    SitePhotoArchiveReason Reason,
    string Note,
    string? ProjectId,
    DateOnly? PeriodEnd,
    string ArchivedByEmail,
    DateTimeOffset ArchivedAt);

public static class SitePhotoArchiveReasons
{
    public static string Label(this SitePhotoArchiveReason reason) => reason switch
    {
        SitePhotoArchiveReason.NotProgress => "Not progress",
        SitePhotoArchiveReason.NoValue => "No value",
        SitePhotoArchiveReason.DocumentOrDrawing => "Document or drawing",
        SitePhotoArchiveReason.Screenshot => "Screenshot",
        SitePhotoArchiveReason.DrawnOver => "Drawn over",
        SitePhotoArchiveReason.SnagOrDefect => "Snag or defect",
        _ => "Other"
    };
}
