using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Features.Triage;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- The Outbox: replies lined up against OLDER emails. Each anchor email is first tagged
    //      to the triage's record picks (one triage decision covers every email answered —
    //      decision 2026-08-12), then the reply sends; the server files the sent copy by the
    //      anchor's tags, the fresh ones included, because the links land before the send.
    //      MessageOnly spread: the reply answers THAT email; the selected email's thread
    //      decision doesn't reach into other conversations. Each entry is removed as it
    //      completes, so a failure stops the apply with the already-sent replies never re-sent. ----
    private async Task SendQueuedRepliesAsync(ApplyPlan plan, bool saveAsDraftOnly)
    {
        var outboxSent = 0;
        foreach (var lined in queuedReplies.ToList())
        {
            await TagLinedUpAnchorAsync(lined, plan);
            busyLabel = saveAsDraftOnly ? "Saving lined-up drafts" : "Sending lined-up emails";
            await Intake.SendComposedEmailAsync(LinedUpCommand(lined, saveAsDraftOnly), MailCompose.UploadPartsOf(lined.Attachments));
            queuedReplies.Remove(lined);
            outboxSent++;
        }
        if (outboxSent > 0)
            outboxNote = saveAsDraftOnly
                ? $"{outboxSent} lined-up {(outboxSent == 1 ? "email was" : "emails were")} saved to the mailbox's Drafts — review and send from Outlook."
                : $"{outboxSent} lined-up {(outboxSent == 1 ? "email was" : "emails were")} sent from the projects mailbox{(plan.Picks.Count > 0 ? ", each email tagged to the picked records" : "")}.";
    }

    private async Task TagLinedUpAnchorAsync(StagedOutboxReply lined, ApplyPlan plan)
    {
        foreach (var record in plan.Picks)
        {
            busyLabel = "Tagging lined-up replies";
            await Intake.LinkMessageToRecordAsync(
                lined.MessageId, lined.InternetMessageId, record.Type, record.RecordId,
                pathway: CostCentrePathwayFor(record),
                allowCrossPathway: true,
                scope: LinkThreadScope.MessageOnly);
        }
    }

    // MarkThreadHandled off: the anchor is an already-triaged email — its record tags
    // say more than Replied would, and it isn't sitting in the queue to clear. A
    // lined-up FORWARD routes through Graph's createForward server-side (Forward).
    private static SendMailboxEmail LinedUpCommand(StagedOutboxReply lined, bool saveAsDraftOnly) => new(
        ReplyToMessageId: lined.MessageId,
        ReplyToInternetMessageId: lined.InternetMessageId,
        To: MailCompose.ParseRecipients(lined.ToField),
        Cc: MailCompose.ParseRecipients(lined.CcField),
        Bcc: MailCompose.ParseRecipients(lined.BccField),
        Subject: lined.Subject.Trim(),
        Body: lined.Body,
        BodyIsHtml: true,
        Attachments: lined.Attachments.Select(a => a.ToRef()).ToList(),
        SaveAsDraftOnly: saveAsDraftOnly,
        Pathway: null,
        MarkThreadHandled: false,
        Forward: lined.IsForward);

    // ---- Section 1: the reply (or forward) — last, so nothing above can be lost to a send
    //      failure. When a filing already dealt with the thread its record tag says more than
    //      Replied, so the stamp is skipped — and a FORWARD never stamps: it passes the email on
    //      rather than answering it, so the email stays queued unless a filing dealt with it. ----
    private async Task SendOpenReplyAsync(
        ApplyPlan plan, bool saveAsDraftOnly, bool filed,
        IReadOnlyList<(string PartName, IBrowserFile File)> uploadParts)
    {
        if (!plan.Replying) return;
        var anchor = plan.Anchor!;
        busyLabel = saveAsDraftOnly ? "Saving draft" : (replyIsForward ? "Sending forward" : "Sending reply");
        var command = new SendMailboxEmail(
            ReplyToMessageId: anchor.Id,
            ReplyToInternetMessageId: anchor.InternetMessageId,
            To: ParseRecipients(replyToField),
            Cc: ParseRecipients(replyCcField),
            Bcc: ParseRecipients(replyBccField),
            Subject: replySubject.Trim(),
            Body: replyBody,
            BodyIsHtml: true,
            Attachments: replyAttachments.Select(a => a.ToRef()).ToList(),
            SaveAsDraftOnly: saveAsDraftOnly,
            Pathway: pathway?.ToString(),
            MarkThreadHandled: !filed && !replyIsForward,
            Forward: replyIsForward);
        composeOutcome = await Intake.SendComposedEmailAsync(command, uploadParts);
        replyBody = "";
        replyOpen = false;
        replyIsForward = false;
        replyAttachments = Array.Empty<ComposeDraftAttachment>();
    }

    // Applied in full: refresh the queue in place — the triager stays on the page they were
    // working — and clear the selection (the email has left it). The Triage tab hands back to
    // the queue list, ready for the next email.
    private async Task CloseOutApplyAsync()
    {
        await Task.WhenAll(ReloadQueueInPlaceAsync(), LoadRecentTriageAsync());
        selected = null;
        detail = null;
        detailLoading = false;
        discardArmed = false;
        stagedCreate = null;
        createdNowRecords.Clear();
        relevantEventStaged = null;
        triageEntireThread = null;
        useThreadTags = null;
        pickedRecords.Clear();
        stagedSystemActions.Clear();
        ReturnWorkspaceToQueue();
    }
}
