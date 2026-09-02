using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class SendMailboxEmailHandler
{
    /// <summary>The draft in the mailbox's Drafts folder with its categories, attachments and —
    /// for a reply or forward — the composer's envelope in place of Graph's scaffolding.</summary>
    private async Task StageDraftAsync(Compose compose, CancellationToken cancellationToken)
    {
        SettleCategories(compose);
        if (compose.IsReply)
            await StageReplyDraftAsync(compose, cancellationToken);
        else
            await StageNewDraftAsync(compose, cancellationToken);
    }

    // Categories on the draft = what the SENT COPY should carry, so it self-files. A reply is
    // part of the same correspondence as the email it answers, so it INHERITS every record and
    // to-do tag the inbound thread carries at send time (the triage apply files the thread
    // BEFORE sending, so tags applied in the same action are already on the anchor). That is
    // what makes the outbound leg appear in a linked record's communications and in a to-do's
    // linked-emails list — those views read the mailbox live by tag, and thread-tagging never
    // sweeps messages that arrive after the decision, which the sent copy always does.
    // With no record involvement, a handled reply carries Replied; a brand-new email with no
    // record chosen carries no categories at all (a pathway tag without a workflow tag would
    // violate the bucket invariant, and Sent Items never queues anyway).
    private static void SettleCategories(Compose compose)
    {
        var inheritedTags = (compose.Snapshot?.Categories ?? Array.Empty<string>())
            .Where(c => TriageCategories.IsWorkflowTag(c)
                && !TriageCategories.IsBucketTag(c)
                && !c.Equals(TriageCategories.Discarded, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var workflowStamp = new List<string>();
        if (compose.RecordTag is not null) workflowStamp.Add(compose.RecordTag);
        workflowStamp.AddRange(inheritedTags.Where(t => !workflowStamp.Contains(t, StringComparer.OrdinalIgnoreCase)));
        if (workflowStamp.Count == 0 && compose.WillHandleThread)
            workflowStamp.Add(TriageCategories.Replied);
        compose.WorkflowStamp = workflowStamp;

        var draftCategories = new List<string>();
        if (workflowStamp.Count > 0)
        {
            draftCategories.Add(TriageCategories.Marker);
            draftCategories.AddRange(workflowStamp);
            if (compose.EffectiveBucket is not null) draftCategories.Add(compose.EffectiveBucket);
        }
        compose.DraftCategories = draftCategories;
    }

    private async Task StageReplyDraftAsync(Compose compose, CancellationToken cancellationToken)
    {
        var command = compose.Command;
        var replyDraft = await graph.CreateReplyDraftAsync(
            new MailboxReplyDraftMessage(
                command.ReplyToMessageId!,
                HtmlCoverNote: compose.BodyHtml,
                Attachments: compose.Attachments,
                Categories: compose.DraftCategories.Count == 0 ? null : compose.DraftCategories,
                Forward: compose.IsForward),
            cancellationToken);
        if (replyDraft is null)
        {
            await RollBackRaisedRequestAsync(compose.RaisedRequest, compose.RecordTag, cancellationToken);
            throw new InvalidOperationException(
                $"The {(compose.IsForward ? "forward" : "reply")} couldn't be staged in the projects mailbox, so nothing was sent and nothing was triaged. "
                + "The original email may no longer be there, or the mailbox connection failed — check and try again.");
        }
        compose.DraftId = replyDraft.Id;
        compose.WebLink = replyDraft.WebLink;
        await ApplyEnvelopeAsync(compose, cancellationToken);
    }

    // The composer's envelope is authoritative — replace Graph's reply-all scaffolding with
    // exactly what the user saw. Nothing is added server-side: the projects mailbox is
    // never auto-Cc'd (decision 2026-08-07 — a delivered Cc copy arrives back in the Inbox
    // untagged and lands straight back in the triage queue).
    private async Task ApplyEnvelopeAsync(Compose compose, CancellationToken cancellationToken)
    {
        var applied = await graph.UpdateDraftEnvelopeAsync(
            compose.DraftId, compose.DraftTo, compose.DraftCc,
            compose.DraftBcc, compose.Subject, cancellationToken);
        if (applied) return;
        await RollBackRaisedRequestAsync(compose.RaisedRequest, compose.RecordTag, cancellationToken);
        throw new InvalidOperationException(
            "The recipients couldn't be applied to the draft, so nothing was sent. "
            + "A partial draft may remain in the mailbox's Drafts folder — check and try again.");
    }

    private async Task StageNewDraftAsync(Compose compose, CancellationToken cancellationToken)
    {
        var draft = await graph.CreateDraftAsync(
            new MailboxDraftMessage(
                compose.DraftTo, compose.Subject,
                compose.BodyHtml, compose.Attachments,
                Bcc: compose.Bcc.Count == 0 ? null : compose.DraftBcc,
                Categories: compose.DraftCategories.Count == 0 ? null : compose.DraftCategories,
                Cc: compose.Cc.Count == 0 ? null : compose.DraftCc),
            cancellationToken);
        if (draft is null)
            throw new InvalidOperationException(
                "The email couldn't be staged in the projects mailbox, so nothing was sent. "
                + "The mailbox connection may have failed — check and try again.");
        compose.DraftId = draft.Id;
        compose.WebLink = draft.WebLink;
    }
}
