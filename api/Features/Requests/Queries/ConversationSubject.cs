using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// The part of an email subject that survives replying and forwarding — the basis for finding a
/// thread's members when Graph's ConversationId lets us down (Outlook mints a new id when a
/// subject is edited, when a message is forwarded from some clients, or when the mailbox's copy
/// is missing the original). "Re: FW: Re: 17A Abbot Road – Ply issue" and "RE: 17A Abbot Road -
/// Ply issue" both fold to "17a abbot road ply issue".
/// </summary>
public static partial class ConversationSubject
{
    /// <summary>Subjects shorter than this fold to nothing useful — a search on them would pull in
    /// unrelated mail, so the fallback stands down.</summary>
    private const int MinimumUsableLength = 8;

    public static string Normalise(string? subject)
    {
        var text = Punctuation().Replace(StripPrefixes(subject), " ");
        return text.Trim().ToLowerInvariant();
    }

    /// <summary>The subject with only the reply/forward prefixes removed — what the mailbox search
    /// is given, since its phrase search wants the subject as the sender wrote it.</summary>
    public static string StripPrefixes(string? subject) =>
        ReplyForwardPrefixes().Replace((subject ?? "").Trim(), "").Trim();

    public static bool IsUsable(string normalisedSubject) => normalisedSubject.Length >= MinimumUsableLength;

    public static bool SameThread(string? subjectA, string? subjectB) =>
        Normalise(subjectA) == Normalise(subjectB);

    [GeneratedRegex(@"^(\s*((re|fw|fwd|aw|wg|tr|sv)(\[\d+\])?\s*:\s*)|\s*\[external\]\s*)+", RegexOptions.IgnoreCase)]
    private static partial Regex ReplyForwardPrefixes();

    [GeneratedRegex(@"[^\p{L}\p{Nd}]+")]
    private static partial Regex Punctuation();
}
