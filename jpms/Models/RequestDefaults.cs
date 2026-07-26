namespace Jewel.JPMS.Models;

// Shared defaults for the "raise a request" forms — the triage create panel and RaiseRequestDialog.
// One place so both routes into a request offer the same response window; a request raised from an
// email and one raised from the project page should never disagree about what "due" means.
public static class RequestDefaults
{
    // The house standard: an answer is expected inside a week. Applied as a pre-filled value only —
    // the field stays editable and the server has no opinion, so overriding it (or clearing it) is
    // always allowed.
    public const int ResponseWindowDays = 7;

    // Pre-fill value for an <input type="date"> bound as a string. Local date, since the triager
    // reads it as "next Tuesday" rather than as an instant.
    public static string ResponseDue() =>
        DateTime.Today.AddDays(ResponseWindowDays).ToString("yyyy-MM-dd");
}
