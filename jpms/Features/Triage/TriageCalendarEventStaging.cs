using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// The calendar event drafted by "Raise Calendar Event" in System Actions: an email arranging
/// something dated — a site visit, a delivery slot, a meeting — becomes an event on the email's
/// project, with the email tagged to it (JPMS/CAL-####) so the event reads the arranging mail
/// back live. Dates are held as the inputs' own text ("yyyy-MM-dd" / "HH:mm") until apply, so a
/// half-typed value never throws — the StagedRecordCreate arrangement.
/// </summary>
public sealed class StagedCalendarEventDraft
{
    public string Title { get; set; } = "";
    public CalendarEventKind Kind { get; set; } = CalendarEventKind.Meeting;
    // New drafts start a week out — the house default for something arranged today (the
    // TodoDraftRow rule). No date is read out of the email body: nothing extracts dates from
    // prose anywhere, and a wrong guess on a calendar is worse than a default.
    public string Date { get; set; } = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
    public string StartTime { get; set; } = "";
    public string EndDate { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool ClientVisible { get; set; }

    /// <summary>What still stops the event being raised — null when it is complete. Shared by
    /// the editor (inline hint) and the page's Apply (hard gate), so the wording is decided
    /// once — the same "decision not yet made" rule as the staged work order and defect.</summary>
    public string? Problem
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Title)) return "Give the event a title.";
            if (ParsedDate is null) return "Give the event a date.";
            // The same reading the server applies (CalendarStartTime), so a time the server
            // would refuse is caught HERE, in the pane, before Apply ever sends the command.
            if (!CalendarStartTime.TryNormalise(StartTime, out _))
                return "The start time isn't a time — try 09:30, or leave it blank for all-day.";
            if (!string.IsNullOrWhiteSpace(EndDate) && ParsedEndDate is null) return "The end date isn't a date.";
            if (ParsedEndDate is { } end && end < ParsedDate) return "End date can't be before the start date.";
            return null;
        }
    }

    public string Outcome => "raise the calendar event on the email's project and tag this email to it";

    public DateTime? ParsedDate => ParseDate(Date);
    public DateTime? ParsedEndDate => ParseDate(EndDate);

    public CreateCalendarEventFromMessage ToCommand(
        string messageId, string? internetMessageId, string projectId, LinkThreadScope scope, bool allowCrossPathway)
    {
        var date = ParsedDate ?? DateTime.Today;
        var end = ParsedEndDate;
        // Canonical "HH:mm" (null = all-day) — Problem has already refused anything unreadable.
        CalendarStartTime.TryNormalise(StartTime, out var startTime);
        return new CreateCalendarEventFromMessage(
            messageId,
            internetMessageId,
            projectId,
            new CalendarEventDetails(
                Title.Trim(),
                Kind,
                new DateTimeOffset(date, TimeSpan.Zero),
                startTime,
                end is { } endDate && endDate != date ? new DateTimeOffset(endDate, TimeSpan.Zero) : null,
                Notes.Trim(),
                ClientVisible),
            Scope: scope,
            AllowCrossPathway: allowCrossPathway);
    }

    private static DateTime? ParseDate(string text) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var value)
            ? value
            : null;
}
