using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>The day's note as the worker's log writes it onto the project's progress feed: titled
/// for the person and in their own name, so the office and the Contractor's Report read who
/// filed, and photographs may be added only by the person whose note it is.</summary>
public static class MyDayNotes
{
    private const string LogPrefix = "Daily log — ";
    private const string OffPrefix = "Off — ";

    public static string LogTitle(WorkerEntity worker) => LogPrefix + worker.Name;

    public static string OffTitle(WorkerEntity worker) => OffPrefix + worker.Name;

    public static bool IsGiven(string description) => !string.IsNullOrWhiteSpace(description);

    public static bool IsOwnedBy(ProgressUpdateEntity note, string email) =>
        string.Equals(note.CreatedByEmail, email, StringComparison.OrdinalIgnoreCase);
}
