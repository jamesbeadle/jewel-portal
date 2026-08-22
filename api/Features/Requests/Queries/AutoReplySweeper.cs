using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// Out-of-office and other automatic replies never need a triage decision (Nigel, 2026-08-22 —
/// they were sitting in the queue between real emails). As the queue is listed, any such email
/// is tagged Discarded — THAT ONE MESSAGE ONLY, never its thread: an auto-reply shares its
/// conversation with the real email it answers, and discarding that would be a real loss. It
/// then drops out of the page so the triager never sees it; the Discarded view keeps it, and
/// Restore brings it back like any discard. Best-effort: a tag that fails leaves the email in the
/// queue for a human.
/// </summary>
public sealed class AutoReplySweeper
{
    private static readonly string[] AutomaticSubjectStarts =
    {
        "automatic reply", "auto reply", "auto-reply", "autoreply", "out of office", "out of the office",
        "automatische antwort", "réponse automatique", "risposta automatica", "respuesta automática",
    };

    private readonly IMailboxGraphClient graph;
    private readonly ILogger<AutoReplySweeper> logger;

    public AutoReplySweeper(IMailboxGraphClient graph, ILogger<AutoReplySweeper> logger)
    {
        this.graph = graph;
        this.logger = logger;
    }

    public static bool IsAutomaticReply(MailboxMessage email)
    {
        var subject = email.Subject.Trim();
        return AutomaticSubjectStarts.Any(start => subject.StartsWith(start, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>How many of the page's emails were automatic replies and are now discarded. The
    /// caller re-reads the page when this is above zero: the inbox cursor is an offset into the
    /// untagged set, and a discard shrinks that set, so the page as listed has gaps.</summary>
    public async Task<int> SweepAsync(MailboxPage page, CancellationToken cancellationToken)
    {
        var discarded = 0;
        foreach (var email in page.Items.Where(IsAutomaticReply))
        {
            if (await TryDiscardAsync(email, cancellationToken)) discarded++;
        }
        return discarded;
    }

    private async Task<bool> TryDiscardAsync(MailboxMessage email, CancellationToken cancellationToken)
    {
        try
        {
            var discarded = await graph.DiscardAsync(email.Id, email.InternetMessageId, cancellationToken);
            if (discarded) logger.LogInformation("Auto-reply discarded from the queue: {Subject}", email.Subject);
            return discarded;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Auto-reply could not be discarded; left in the queue: {Subject}", email.Subject);
            return false;
        }
    }
}
