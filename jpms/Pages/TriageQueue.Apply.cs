namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ONE Send: applies whatever filing is set up in the panes above (record links, a new
    // record, to-dos), then sends the reply. Filing and replying are two halves of dealing with
    // an email — deliberately combinable, never forced apart. Filing runs first (each command
    // verifies its tags before saving); the send comes last so a filing failure stops everything
    // with the email still queued, and a send failure leaves the thread filed with the reply
    // safe in Drafts (the outcome banner says exactly which). ApplyStepsAsync owns the order;
    // this owns the gate, the one busy flag and the catch.
    private async Task DoApplyAll(bool saveAsDraftOnly)
    {
        if (busy) return;
        // Lined-up Outbox replies apply on their own — no selection needed (decision 2026-08-12).
        if (selected is null && queuedReplies.Count == 0) return;

        var plan = BuildApplyPlan();
        if (!ApplyHasWork(plan)) return;
        if (ApplyRefusal(plan) is { } refusal) { actionError = refusal; return; }

        var uploadParts = UploadPartsOf(replyAttachments);
        actionError = null;
        busy = true;
        try
        {
            await ApplyStepsAsync(plan, saveAsDraftOnly, uploadParts);
        }
        catch (CommandFailedException ex)
        {
            actionError = ex.Message;
        }
        catch
        {
            actionError = "That didn't complete. Please try again.";
        }
        finally { busy = false; }
    }

    private async Task ApplyStepsAsync(
        ApplyPlan plan, bool saveAsDraftOnly,
        IReadOnlyList<(string PartName, IBrowserFile File)> uploadParts)
    {
        var filed = false;

        var inheritedRecords = await ResolveInheritedRecordsAsync(plan);
        if (inheritedRecords is null) return;

        await SendDocTriageAttachmentsAsync(plan);
        filed |= await RaiseTodoDraftsAsync(plan);
        filed |= await LinkPickedRecordsAsync(plan);
        filed |= await LinkInheritedRecordsAsync(plan, inheritedRecords);
        filed |= await TagRelevantEventAsync(plan);

        var created = await RaiseStagedCreateAsync(plan);
        if (created is null) return;
        filed |= created.Value;

        await RunStagedSystemActionsAsync(plan);
        filed |= await DiscardAnchorAsync(plan);
        await SendQueuedRepliesAsync(plan, saveAsDraftOnly);
        await SendOpenReplyAsync(plan, saveAsDraftOnly, filed, uploadParts);

        await CloseOutApplyAsync();
    }

    // The staged record, raised once per apply. Null stops the apply: the order exists and the
    // email is tagged to it — never re-raise it — but its picked files failed to upload, so the
    // failure is seen before any reply sends; the email stays selected and the files can be
    // re-added from the order's PO page.
    private async Task<bool?> RaiseStagedCreateAsync(ApplyPlan plan)
    {
        if (!plan.CreateReady || stagedCreate is not { } staged) return false;
        var created = await RaiseStagedRecordAsync(staged, plan.Anchor!, plan.Scope);
        stagedCreate = null;
        createdNowRecords.Add(created.Record);
        if (created.UploadError is null) return true;
        actionError = created.UploadError;
        return null;
    }
}
