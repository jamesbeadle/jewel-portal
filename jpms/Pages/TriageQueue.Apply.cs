using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ONE Send: applies whatever filing is set up in the panes above (record links, a new
    // record, to-dos), then sends the reply. Filing and replying are two halves of dealing with
    // an email — deliberately combinable, never forced apart. Filing runs first (each command
    // verifies its tags before saving); the send comes last so a filing failure stops everything
    // with the email still queued, and a send failure leaves the thread filed with the reply
    // safe in Drafts (the outcome banner says exactly which). Each numbered step below is its
    // own method; this orchestrator owns only the order, the one busy flag and the catch.
    private async Task DoApplyAll(bool saveAsDraftOnly)
    {
        if (busy) return;
        // Lined-up Outbox replies apply on their own — no selection needed (decision 2026-08-12).
        if (selected is null && queuedReplies.Count == 0) return;

        var plan = BuildApplyPlan();
        if (!ApplyHasWork(plan)) return;
        if (ApplyRefusal(plan) is { } refusal)
        {
            actionError = refusal;
            return;
        }

        var uploadParts = UploadPartsOf(replyAttachments);
        actionError = null;
        busy = true;
        try
        {
            var filed = false;

            var inheritedRecords = await ResolveInheritedRecordsAsync(plan);
            if (inheritedRecords is null) return;

            await SendDocTriageAttachmentsAsync(plan);
            filed |= await RaiseTodoDraftsAsync(plan);
            filed |= await LinkPickedRecordsAsync(plan);
            filed |= await LinkInheritedRecordsAsync(plan, inheritedRecords);
            filed |= await TagRelevantEventAsync(plan);

            if (plan.CreateReady && stagedCreate is { } staged)
            {
                var created = await RaiseStagedRecordAsync(staged, plan.Anchor!, plan.Scope);
                // One create per apply: clear it so nothing can double-create.
                stagedCreate = null;
                createdNowRecords.Add(created.Record);
                filed = true;
                if (created.UploadError is not null)
                {
                    // The order exists and the email is tagged to it — never re-raise it. Stop
                    // here (before any reply sends) so the failure is seen; the email stays
                    // selected and the files can be re-added from the order's PO page.
                    actionError = created.UploadError;
                    return;
                }
            }

            await RunStagedSystemActionsAsync();
            filed |= await DiscardAnchorAsync(plan);
            await SendQueuedRepliesAsync(plan, saveAsDraftOnly);
            await SendOpenReplyAsync(plan, saveAsDraftOnly, filed, uploadParts);

            await CloseOutApplyAsync();
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

    /// <summary>One apply's inputs, captured at the button press so every step and every refusal
    /// reads the same snapshot the triager saw.</summary>
    private sealed record ApplyPlan(
        MailboxMessage? Anchor,
        bool Replying,
        List<TodoItemDraft> Drafts,
        List<LinkableRecord> Picks,
        bool CreateReady,
        bool RelevantEvent,
        bool Discarding,
        List<string> InheritStems,
        LinkThreadScope Scope,
        bool CreatedNowOnly);

    private ApplyPlan BuildApplyPlan()
    {
        var anchorEmail = selected;
        return new ApplyPlan(
            Anchor: anchorEmail,
            Replying: ReplyDraftPending && anchorEmail is not null,
            Drafts: anchorEmail is null ? new List<TodoItemDraft>() : CurrentTodoDrafts(),
            Picks: pickedRecords.ToList(),
            CreateReady: anchorEmail is not null && StagedCreateReady
                && (!string.IsNullOrWhiteSpace(triageProjectId) || StagedCreatesOwnProject),
            RelevantEvent: relevantEventStaged == true && anchorEmail is not null,
            Discarding: discardArmed && anchorEmail is not null,
            // "Use existing tags" answered Yes: the thread's tag stems, captured now so the apply
            // works from what the triager saw. Resolved to records inside the try — before
            // anything else lands — and linked exactly like picks.
            InheritStems: useThreadTags == true && anchorEmail is not null
                ? SelectedThreadTags.ToList()
                : new List<string>(),
            // One scope for the whole apply: a thread-wide Yes opts every staged action into the thread.
            Scope: triageEntireThread == true ? LinkThreadScope.EntireThread : LinkThreadScope.MessageOnly,
            // A create-now-only triage: the record is already raised and the email tagged to it
            // (Create now did both), so there is nothing left to RUN — but the apply still owns
            // the close-out (queue reload, selection cleared). Letting it through is what
            // un-sticks the Apply button after Create now; every step no-ops on its own guard.
            CreatedNowOnly: anchorEmail is not null && createdNowRecords.Count > 0);
    }

    private bool ApplyHasWork(ApplyPlan plan) =>
        plan.Replying || plan.CreateReady || plan.RelevantEvent || plan.Discarding
        || plan.Drafts.Count > 0
        || plan.Picks.Count > 0
        || stagedSystemActions.Count > 0 || queuedReplies.Count > 0
        || stagedDocControlIds.Count > 0
        || plan.InheritStems.Count > 0
        || plan.CreatedNowOnly;

    // Every way an apply refuses to run, in one gauntlet — the reason to show, or null to
    // proceed. The bar's Yes/No pairs start blank on purpose (an apply with any unanswered is a
    // decision not yet made), and every staged half-decision is finish-it-or-clear-it rather
    // than quietly skipped: belt-and-braces behind the disabled button, so no other route into
    // the apply can land with a blank answer.
    private string? ApplyRefusal(ApplyPlan plan)
    {
        if (plan.Anchor is not null && MissingDecisionNames() is { Count: > 0 } missingDecisions)
            return $"Answer {AndJoin(missingDecisions)} — Yes or No — then Apply.";

        // A half-built lined-up email — finish it or remove it, rather than have Apply skip it
        // (or the server reject it after the filing has already landed).
        if (queuedReplies.FirstOrDefault(lined => lined.Problem is not null) is { } notReady)
            return $"A lined-up {(notReady.IsForward ? "forward" : "reply")} ({notReady.AnchorSubject}) isn't ready — {notReady.Problem} Finish it in the Outbox, or remove it.";

        // A reply and a discard contradict each other — an email worth answering isn't spam.
        // (Same rule for an unsent forward: send or discard the draft before binning the email.)
        if (plan.Discarding && plan.Replying)
            return $"Discard and a {(replyIsForward ? "forward" : "reply")} don't mix — send (or discard) the draft first.";
        if (plan.Discarding && plan.Picks.Count > 0)
            return "Discard and record links don't mix — unpick the records first.";
        if (plan.Discarding && plan.InheritStems.Count > 0)
            return "Discard and the thread's existing tags don't mix — answer No to Use existing tags, or disarm the discard.";
        if (plan.Discarding && plan.RelevantEvent)
            return "Discard and a Relevant Event tag don't mix — answer No to Relevant Event, or disarm the discard.";

        if (plan.RelevantEvent && string.IsNullOrWhiteSpace(triageProjectId))
            return "To tag a Relevant Event for the Programme, set the email's Project first — or answer No.";

        // Attachments bound for Document Triage without a project (decision 2026-08-28): an
        // unassigned file in the queue is as good as discarded.
        if (plan.Anchor is not null && stagedDocControlIds.Count > 0 && string.IsNullOrWhiteSpace(triageProjectId))
            return "To send attachments to Document Triage, set the email's Project first — or untick them.";

        if (StagedCreateReady && !plan.CreateReady)
            return "To create the record, set the email's Project first — or remove the staged record in the pathway pane's Actions.";
        // A staged record that isn't complete yet (no subcontractor, no priced line, no
        // description…) — finish it or clear it, rather than let the server reject a half-built
        // record after the to-dos have already been raised.
        if (plan.CreateReady && stagedCreate is { Kind: StagedRecordKind.WorkOrder } stagedOrder
            && stagedOrder.WorkOrderProblem is { } orderProblem)
            return $"The staged work order isn't ready — {orderProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (plan.CreateReady && stagedCreate is { Kind: StagedRecordKind.Defect } stagedDefect
            && stagedDefect.DefectProblem is { } defectProblem)
            return $"The staged defect isn't ready — {defectProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (plan.CreateReady && stagedCreate is { Kind: StagedRecordKind.Inventory } stagedInventory
            && stagedInventory.InventoryProblem is { } inventoryProblem)
            return $"The staged inventory item isn't ready — {inventoryProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (plan.CreateReady && StagedTenderEnquiryProblem is { } enquiryProblem)
            return $"The staged tender enquiry isn't ready — {enquiryProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (plan.CreateReady && StagedCalendarEventProblem is { } calendarProblem)
            return $"The staged calendar event isn't ready — {calendarProblem} Finish it in the pathway pane's Actions, or remove it.";
        if (plan.CreateReady && StagedBuildingControlInspectionProblem is { } inspectionProblem)
            return $"The staged inspection isn't ready — {inspectionProblem} Finish it in the pathway pane's Actions, or remove it.";

        if (plan.Replying)
        {
            if (ParseRecipients(replyToField).Count == 0) return "Add a To recipient to the reply.";
            if (string.IsNullOrWhiteSpace(replySubject)) return "Write a subject for the reply.";
            // A reply alone triages the thread as Replied — pathway-less is fine (answering IS
            // dealing with it); choosing a tab in System Tags files it under that side as well.
        }
        return null;
    }

    // ---- "Use existing tags" answered Yes: resolve the thread's tag stems back to records
    //      FIRST (the same ResolveRecordTags behind the search chips), so a stem that no longer
    //      names anything stops the apply before anything else lands — the same
    //      every-tag-verified-before-anything-saves rule as the rest of the filing. The links
    //      themselves land with the picks. Null = stop the apply (the error is set). ----
    private async Task<IReadOnlyList<LinkableRecord>?> ResolveInheritedRecordsAsync(ApplyPlan plan)
    {
        var stems = plan.InheritStems;
        if (plan.Anchor is null || stems.Count == 0) return Array.Empty<LinkableRecord>();
        busyLabel = "Matching the thread's tags";
        var inherited = await Queries.AskAsync(
            new ResolveRecordTags(stems.Select(TriageEmailDisplay.TagLabel).ToList()), CancellationToken.None);
        if (inherited.Count > 0) return inherited;
        actionError = "The thread's existing tags couldn't be matched to records — pick this email's records by hand instead.";
        return null;
    }

    // ---- Document Triage: ticked attachments copy out FIRST, so the files are safely in the
    //      queue before anything else (a discard included) moves the email on. Never consumes
    //      the email — only the files are copied out; `filed` deliberately stays untouched. ----
    private async Task SendDocTriageAttachmentsAsync(ApplyPlan plan)
    {
        if (plan.Anchor is not { } anchor || stagedDocControlIds.Count == 0) return;
        busyLabel = "Sending to Document Triage";
        await Commands.SendAsync(
            new SendAttachmentsToDocumentControl(
                anchor.Id, anchor.InternetMessageId,
                stagedDocControlIds.ToList(), NullIfBlank(triageProjectId)),
            CancellationToken.None);
        // One send per apply: clear the ticks (the server skips already-sent ids regardless).
        stagedDocControlIds.Clear();
    }

    // ---- Section 2: to-dos (their command verifies every tag before saving) ----
    private async Task<bool> RaiseTodoDraftsAsync(ApplyPlan plan)
    {
        if (plan.Drafts.Count == 0) return false;
        var anchor = plan.Anchor!;
        busyLabel = "Creating to-dos";
        // No request link here: to-dos are their own concern, and linking the email to a
        // record — a request included — is the filing section's job.
        await Intake.CreateTodoItemsFromMessageAsync(new CreateTodoItemsFromMessage(
            anchor.Id,
            NullIfBlank(triageProjectId),
            plan.Drafts,
            LinkRequestId: null,
            InternetMessageId: anchor.InternetMessageId,
            Pathway: pathway is { } chosenForTodos ? TriagePathways.Label(chosenForTodos) : null,
            Scope: plan.Scope));
        // One batch per apply: clear the rows so nothing can double-raise.
        createTodoRows = new List<TodoDraftRow> { new() };
        return true;
    }

    // ---- Record filing: every staged link applies, whatever picker is open ----
    private async Task<bool> LinkPickedRecordsAsync(ApplyPlan plan)
    {
        if (plan.Anchor is not { } anchor || plan.Picks.Count == 0) return false;
        busyLabel = "Linking";
        foreach (var record in plan.Picks)
        {
            // AllowCrossPathway: true — the pane choice IS the cross-filing decision
            // (confirm retired 2026-08-28; true also keeps an older api from prompting).
            await Intake.LinkMessageToRecordAsync(
                anchor.Id, anchor.InternetMessageId, record.Type, record.RecordId,
                pathway: CostCentrePathwayFor(record),
                allowCrossPathway: true,
                scope: plan.Scope);
        }
        return true;
    }

    // ---- The thread's existing tags, answered Yes: each resolved record links exactly like a
    //      picked one. Records the triager ALSO picked by hand are skipped — one link per record
    //      per apply. allowCrossPathway is true outright: these tags are already on the thread,
    //      so re-filing this reply under them is never a new cross-pathway decision. ----
    private async Task<bool> LinkInheritedRecordsAsync(ApplyPlan plan, IReadOnlyList<LinkableRecord> inheritedRecords)
    {
        if (plan.Anchor is not { } anchor || inheritedRecords.Count == 0) return false;
        busyLabel = "Linking to the thread's tags";
        var picks = plan.Picks;
        var linked = false;
        foreach (var record in inheritedRecords)
        {
            if (picks.Any(pick => pick.Type == record.Type
                && string.Equals(pick.RecordId, record.RecordId, StringComparison.Ordinal)))
                continue;
            await Intake.LinkMessageToRecordAsync(
                anchor.Id, anchor.InternetMessageId, record.Type, record.RecordId,
                pathway: CostCentrePathwayFor(record),
                allowCrossPathway: true,
                scope: plan.Scope);
            linked = true;
        }
        return linked;
    }

    // A Relevant Event answered Yes: link the thread to the project's programme bucket — the
    // record id IS the project id (one bucket per project, SchedulingLinkProvider). Scheduling
    // is a Client-side record, so on a non-client thread this cross-files the thread — allowed
    // without a confirm, like the picks.
    private async Task<bool> TagRelevantEventAsync(ApplyPlan plan)
    {
        if (!plan.RelevantEvent) return false;
        var anchor = plan.Anchor!;
        busyLabel = "Tagging relevant event";
        await Intake.LinkMessageToRecordAsync(
            anchor.Id, anchor.InternetMessageId, RecordType.Scheduling, triageProjectId,
            pathway: null,
            allowCrossPathway: true,
            scope: plan.Scope);
        return true;
    }

    // ---- System actions lined up in the Actions pane — run once the filing above has landed,
    //      each removed as it succeeds so a failed one can be retried without re-running its
    //      predecessors. A failure stops the apply with its reason. ----
    private async Task RunStagedSystemActionsAsync()
    {
        foreach (var stagedAction in stagedSystemActions.ToList())
        {
            busyLabel = $"System action: {SystemActionKinds.Label(stagedAction.Kind)}";
            await stagedAction.ExecuteAsync();
            stagedSystemActions.Remove(stagedAction);
        }
    }

    // "File it as nothing": tag the thread discarded — restorable from the Tagged tab. Runs
    // after the to-dos so "capture the follow-ups, then bin the email" works.
    private async Task<bool> DiscardAnchorAsync(ApplyPlan plan)
    {
        if (!plan.Discarding) return false;
        var anchor = plan.Anchor!;
        busyLabel = "Discarding";
        await Intake.DiscardMessageAsync(anchor.Id, anchor.InternetMessageId);
        return true;
    }

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
            foreach (var record in plan.Picks)
            {
                busyLabel = "Tagging lined-up replies";
                await Intake.LinkMessageToRecordAsync(
                    lined.MessageId, lined.InternetMessageId, record.Type, record.RecordId,
                    pathway: CostCentrePathwayFor(record),
                    allowCrossPathway: true,
                    scope: LinkThreadScope.MessageOnly);
            }
            busyLabel = saveAsDraftOnly ? "Saving lined-up drafts" : "Sending lined-up emails";
            // MarkThreadHandled off: the anchor is an already-triaged email — its record tags
            // say more than Replied would, and it isn't sitting in the queue to clear. A
            // lined-up FORWARD routes through Graph's createForward server-side (Forward).
            var linedCommand = new SendMailboxEmail(
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
            await Intake.SendComposedEmailAsync(linedCommand, MailCompose.UploadPartsOf(lined.Attachments));
            queuedReplies.Remove(lined);
            outboxSent++;
        }
        if (outboxSent > 0)
            outboxNote = saveAsDraftOnly
                ? $"{outboxSent} lined-up {(outboxSent == 1 ? "email was" : "emails were")} saved to the mailbox's Drafts — review and send from Outlook."
                : $"{outboxSent} lined-up {(outboxSent == 1 ? "email was" : "emails were")} sent from the projects mailbox{(plan.Picks.Count > 0 ? ", each email tagged to the picked records" : "")}.";
    }

    // ---- Section 1: the reply (or forward) — last, so nothing above can be lost to a send
    //      failure. When a filing already dealt with the thread its record tag says more than
    //      Replied, so the stamp is skipped — and a FORWARD never stamps: it passes the email on
    //      rather than answering it, so the email stays queued unless a filing dealt with it. ----
    private async Task SendOpenReplyAsync(
        ApplyPlan plan, bool saveAsDraftOnly, bool filed,
        IReadOnlyList<(string PartName, Microsoft.AspNetCore.Components.Forms.IBrowserFile File)> uploadParts)
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

    /// <summary>What one staged-create execution produced: the created-record chip for the pane,
    /// and — for a work order whose picked files failed to upload — the error that stops the
    /// caller (the order exists and the email is tagged; the files are re-added from the order's
    /// PO page).</summary>
}
