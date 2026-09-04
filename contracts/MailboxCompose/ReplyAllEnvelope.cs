using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.MailboxCompose;

/// <summary>
/// The envelope a reply to a mailbox email starts from — the reply-all set the Control Centre's
/// Reply box prefills, computed once here so the connector's get_mailbox_message can hand an AI
/// tool exactly what the page would show. To is the sender (their Reply-To when they set one);
/// Cc is the original To and Cc minus that address and minus the projects mailbox, which the
/// server Cc's on every send anyway; the subject is "RE:"-prefixed once. The page's composer
/// (jpms MailReplyComposer.PrefillReplyEnvelope) carries the same rule — change them together.
/// </summary>
public static class ReplyAllEnvelope
{
    private const string ReplyPrefix = "RE:";
    private const string NoSubject = "(no subject)";

    public static ReplyAllPrefill For(MailboxMessageDetail detail)
    {
        var to = detail.ReplyTo ?? detail.FromEmail;
        var cc = (detail.To ?? Array.Empty<string>())
            .Concat(detail.Cc ?? Array.Empty<string>())
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Where(address => !IsSameAddress(address, to))
            .Where(address => !IsSameAddress(address, detail.MailboxAddress))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new ReplyAllPrefill(to, cc, ReplySubjectFor(detail.Subject));
    }

    /// <summary>"RE: " the subject once — an already-RE'd subject stays as it is.</summary>
    public static string ReplySubjectFor(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return $"{ReplyPrefix} {NoSubject}";
        var trimmed = subject.Trim();
        if (trimmed.StartsWith(ReplyPrefix, StringComparison.OrdinalIgnoreCase)) return trimmed;
        return $"{ReplyPrefix} {trimmed}";
    }

    private static bool IsSameAddress(string address, string? other) =>
        other is not null && address.Equals(other, StringComparison.OrdinalIgnoreCase);
}

/// <summary>What a reply starts from. To is null only when the email carried no readable
/// sender — the caller must then supply one.</summary>
public sealed record ReplyAllPrefill(string? To, IReadOnlyList<string> Cc, string Subject);
