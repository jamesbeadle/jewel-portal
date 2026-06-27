namespace Jewel.JPMS.Models;

// An AI-proposed draft of a new request, inferred from a triaged email by reading its content
// against the live project list. Returned to the triage UI to pre-fill the "Create new request"
// form — never persisted and never auto-submitted. The triager always reviews and confirms.
//
// Available is false when AI suggestion isn't configured (no Anthropic key) or the call failed; in
// that case the UI keeps the plain subject/body fallback it already has. ProjectId is only set when
// the model confidently matched one of the known projects, otherwise null so the triager picks.
public sealed record RequestSuggestion(
    bool Available,
    string? ProjectId,
    RequestType Kind,
    string Title,
    string Description,
    string? RaisedTo = null,
    string? DrawingRef = null,
    DateTimeOffset? ResponseDue = null,
    string? RelatedDrawingSpec = null,
    decimal? Value = null,
    string? Rationale = null)
{
    // A fallback suggestion used when AI is unavailable: keeps whatever subject/body the caller
    // already had so the form still pre-fills exactly as it did before.
    public static RequestSuggestion Unavailable(string title, string description) =>
        new(false, null, RequestType.Rfi, title, description);
}
