using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// The thread read's fallback (Nigel, 2026-08-22 — "broken entire thread"): Graph's ConversationId
/// returned only the opened email although the chain plainly had more, so the members are found by
/// subject instead — a mailbox search on the folded subject, kept to the emails whose own subject
/// folds to the same thing, within a window that rules out last year's namesake.
/// </summary>
public static class ConversationBySubject
{
    private const int SearchTake = 50;
    private static readonly TimeSpan Window = TimeSpan.FromDays(120);

    public static async Task<MailboxPage> FindAsync(
        IMailboxGraphClient graph, MailboxPage byConversation, string? subject, CancellationToken cancellationToken)
    {
        var folded = ConversationSubject.Normalise(subject);
        if (!ConversationSubject.IsUsable(folded))
            return byConversation;

        var found = await graph.SearchAsync(ConversationSubject.StripPrefixes(subject), SearchTake, cancellationToken);
        var anchorReceivedAt = byConversation.Items.FirstOrDefault()?.ReceivedAt ?? DateTimeOffset.UtcNow;
        // De-duplicate on the STABLE id. Graph's $search hands back the same item under a different
        // Graph id encoding (the "AAkALg…" search-store form) from the "AQMk…" id the conversation
        // filter returned, so keying on Id let the one opened email appear twice — "2 emails" on a
        // one-email thread (V27 Rear Patio, 2026-09-03). The conversation read comes first in the
        // Concat, so the survivor keeps the canonical id the rest of the page acts on.
        var members = byConversation.Items
            .Concat(found.Items.Where(email => ConversationSubject.SameThread(email.Subject, subject)))
            .Where(email => IsWithinWindow(email, anchorReceivedAt))
            .GroupBy(StableKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(email => email.ReceivedAt)
            .ToList();

        if (members.Count <= byConversation.Items.Count)
            return byConversation;
        return new MailboxPage(members, null, members.Count, MatchedBySubject: true);
    }

    // InternetMessageId is the RFC id every copy of a message shares whatever folder or Graph id
    // encoding it is read through; the Graph id is only the fallback when Graph omits it.
    private static string StableKey(MailboxMessage email)
        => string.IsNullOrWhiteSpace(email.InternetMessageId) ? email.Id : email.InternetMessageId;

    private static bool IsWithinWindow(MailboxMessage email, DateTimeOffset anchorReceivedAt)
    {
        var distance = email.ReceivedAt - anchorReceivedAt;
        return distance.Duration() <= Window;
    }
}
