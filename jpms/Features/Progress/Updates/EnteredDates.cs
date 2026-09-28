namespace Jewel.JPMS.Features.Progress.Updates;

/// <summary>
/// A date as a form holds it and as the portal records it, either way round. A bound date arrives
/// as Kind.Local (the kind of DateTime.Today), and the DateTimeOffset constructor refuses a Local
/// kind that disagrees with the zero offset whenever the UK is on BST, so the kind is settled
/// before the offset is.
/// </summary>
public static class EnteredDates
{
    public static DateTimeOffset AsRecorded(DateTime entered) =>
        new(DateTime.SpecifyKind(entered, DateTimeKind.Unspecified), TimeSpan.Zero);

    public static DateTimeOffset? AsRecorded(DateTime? entered) =>
        entered is { } date ? AsRecorded(date) : null;

    public static DateTime? AsEntered(DateTimeOffset? recorded) => recorded?.DateTime;
}
