using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Workspace;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- The Outbox: replies and forwards lined up against OLDER emails (started from a Reply
    //      or Forward button on a record's correspondence or the subcontractor comms browser),
    //      sent by the one Apply.
    //      Deliberately WORKSPACE-LEVEL, not per-selection staging: they survive moving between
    //      inbox emails, and each anchor email is tagged with whatever System Tags picks are
    //      staged when the apply actually runs (decision 2026-08-12) — one triage decision
    //      covering the open email and every email being answered. ----
    private readonly List<StagedOutboxReply> queuedReplies = new();
    // The older email a Reply or Forward press just chose — the Outbox pane opens its composer
    // for it (outboxComposeAnchorIsForward says which button it was). Cleared (by the pane) when
    // the entry is lined up or the composer discarded.
    private MailboxMessage? outboxComposeAnchor;
    private bool outboxComposeAnchorIsForward;
    // The Outbox badge counts everything Apply will send: lined-up replies + the open email's own.
    private int OutboxSendCount => queuedReplies.Count + (ReplyDraftPending ? 1 : 0);
    // What the last apply sent from the Outbox — shown with the other outcome banners where the
    // cleared selection was; dismissable; cleared on the next selection.
    private string? outboxNote;

    // A Reply (or Forward) pressed on an older email anywhere in the workspace: composing happens
    // in the Outbox pane, opened OPPOSITE the list it came from (like a preview) so thread and
    // reply read side by side — the flow is identical from every entry point.
    private void StartOutboxReply(MailboxMessage message, PanelKind anchor)
    {
        outboxComposeAnchor = message;
        outboxComposeAnchorIsForward = false;
        workspace.ShowOpposite(PanelKind.Outbox, anchor);
    }

    private void StartOutboxForward(MailboxMessage message, PanelKind anchor)
    {
        outboxComposeAnchor = message;
        outboxComposeAnchorIsForward = true;
        workspace.ShowOpposite(PanelKind.Outbox, anchor);
    }

    // "Edit in Email window" on the Outbox's current-reply row — that composer lives under the
    // open email, so open its section and show the Email window beside the Outbox.
    private void ShowCurrentReplyComposer()
    {
        replyOpen = true;
        workspace.ShowOpposite(PanelKind.Email, PanelKind.Outbox);
    }
}
