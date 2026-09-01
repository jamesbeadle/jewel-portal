using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- Parked triage (2026-08-10): navigating away no longer costs drafted work. ----
    // Whatever was staged on the open email — the written reply, the project choice, the picked
    // tags, the lined-up actions and to-dos — is parked under that email's id whenever the
    // selection moves off it, and put back exactly as it was the next time the email is opened.
    // Parking is in-memory only: a page refresh still starts clean ("Save reply as draft" is the
    // deliberate keep), and Apply consumes the work instead of parking it. An armed discard is
    // deliberately NOT parked — a destructive step should never come back pre-armed.
    private readonly Dictionary<string, ParkedTriage> parkedTriageByEmailId = new();

    // Everything drafted against one email, held while the triager reads elsewhere.
    private sealed class ParkedTriage
    {
        public TriagePathway? Pathway { get; init; }
        public string ReplyBody { get; init; } = "";
        public string ReplyToField { get; init; } = "";
        public string ReplyCcField { get; init; } = "";
        public string ReplyBccField { get; init; } = "";
        public string ReplySubject { get; init; } = "";
        public bool ReplyShowBcc { get; init; }
        public bool ReplyOpen { get; init; }
        public bool ReplyIsForward { get; init; }
        public bool ReplyEnvelopePrefilled { get; init; }
        public IReadOnlyList<ComposeDraftAttachment> ReplyAttachments { get; init; } = Array.Empty<ComposeDraftAttachment>();
        public string ProjectId { get; init; } = "";
        public bool ProjectAutoMatched { get; init; }
        public RecordType LinkRecordType { get; init; }
        public IReadOnlyList<LinkableRecord> PickedRecords { get; init; } = Array.Empty<LinkableRecord>();
        public IReadOnlyList<StagedSystemAction> SystemActions { get; init; } = Array.Empty<StagedSystemAction>();
        public StagedRecordCreate? Create { get; init; }
        // Records already raised from this email (Create now / the apply's create) — real
        // server-side facts, so the chips must come back with the email they belong to.
        public IReadOnlyList<CreatedNowRecord> CreatedRecords { get; init; } = Array.Empty<CreatedNowRecord>();
        public List<TodoDraftRow> TodoRows { get; init; } = new();
        // Nullable like the live fields: a parked email keeps its answered-or-blank state, so a
        // deliberate No survives a selection change exactly like a Yes — and an unanswered pair
        // comes back still demanding an answer.
        public bool? RelevantEventStaged { get; init; }
        public bool? TriageEntireThread { get; init; }
        public bool? UseThreadTags { get; init; }
        // Attachment ids ticked "Send to document triage" — drafted against ONE email's
        // attachments, so they must travel with that email like every other draft (the same
        // rule that kept the old save-to-drawings form from leaking under the next email).
        public IReadOnlyList<string> DocControlIds { get; init; } = Array.Empty<string>();
    }

    // Anything the triager has actually set is worth keeping; an untouched email parks nothing.
    private bool HasTriageWorthParking =>
        replyOpen
        || HtmlHasContent(replyBody)
        || replyAttachments.Count > 0
        || pickedRecords.Count > 0
        || stagedSystemActions.Count > 0
        || stagedCreate is not null
        || createdNowRecords.Count > 0
        || createTodoRows.Any(row => !string.IsNullOrWhiteSpace(row.Title) || !string.IsNullOrWhiteSpace(row.Notes))
        // An ANSWERED Yes/No is a decision the triager made — a No as much as a Yes — so either
        // pair being non-null is worth keeping across a selection change.
        || relevantEventStaged is not null
        || triageEntireThread is not null
        || useThreadTags is not null
        || stagedDocControlIds.Count > 0
        // The project counts only when the triager chose it themselves. An auto-matched project
        // (TryPrefillProjectFromEmailAsync's guess from the email text) is the page's doing, not
        // theirs — parking it made untouched emails show a "✎ draft" badge after a mere click-through.
        // Reopening the email re-runs the same auto-match anyway, so nothing is lost by not parking it.
        || (!projectAutoMatched && !string.IsNullOrWhiteSpace(triageProjectId));

    private void ParkSelectedTriage()
    {
        if (selected is null) return;
        if (!HasTriageWorthParking) return;
        parkedTriageByEmailId[selected.Id] = new ParkedTriage
        {
            Pathway = pathway,
            ReplyBody = replyBody,
            ReplyToField = replyToField,
            ReplyCcField = replyCcField,
            ReplyBccField = replyBccField,
            ReplySubject = replySubject,
            ReplyShowBcc = replyShowBcc,
            ReplyOpen = replyOpen,
            ReplyIsForward = replyIsForward,
            ReplyEnvelopePrefilled = replyEnvelopePrefilled,
            ReplyAttachments = replyAttachments,
            ProjectId = triageProjectId,
            ProjectAutoMatched = projectAutoMatched,
            LinkRecordType = linkRecordType,
            PickedRecords = pickedRecords.ToList(),
            SystemActions = stagedSystemActions.ToList(),
            Create = stagedCreate,
            CreatedRecords = createdNowRecords.ToList(),
            TodoRows = createTodoRows,
            RelevantEventStaged = relevantEventStaged,
            TriageEntireThread = triageEntireThread,
            UseThreadTags = useThreadTags,
            DocControlIds = stagedDocControlIds.ToList()
        };
    }

    // Put a parked email's work back exactly as it was left. Returns whether anything came back,
    // so Select knows to refetch the link-record pool the restored picks came from.
    private bool RestoreParkedTriage(string emailId)
    {
        if (!parkedTriageByEmailId.Remove(emailId, out var parked)) return false;
        pathway = parked.Pathway;
        replyBody = parked.ReplyBody;
        replyToField = parked.ReplyToField;
        replyCcField = parked.ReplyCcField;
        replyBccField = parked.ReplyBccField;
        replySubject = parked.ReplySubject;
        replyShowBcc = parked.ReplyShowBcc;
        replyOpen = parked.ReplyOpen;
        replyIsForward = parked.ReplyIsForward;
        replyEnvelopePrefilled = parked.ReplyEnvelopePrefilled;
        replyAttachments = parked.ReplyAttachments;
        triageProjectId = parked.ProjectId;
        projectAutoMatched = parked.ProjectAutoMatched;
        linkRecordType = parked.LinkRecordType;
        pickedRecords.AddRange(parked.PickedRecords);
        stagedSystemActions.AddRange(parked.SystemActions);
        stagedCreate = parked.Create;
        createdNowRecords.AddRange(parked.CreatedRecords);
        createTodoRows = parked.TodoRows;
        relevantEventStaged = parked.RelevantEventStaged;
        triageEntireThread = parked.TriageEntireThread;
        useThreadTags = parked.UseThreadTags;
        stagedDocControlIds.AddRange(parked.DocControlIds);
        return true;
    }

    // LoadLinkRecordsAsync clears the picks because a new pool normally invalidates them —
    // restored picks are the exception, so they are carried over the reload by hand.
    private async Task ReloadLinkRecordsKeepingPicksAsync()
    {
        var restoredPicks = pickedRecords.ToList();
        await LoadLinkRecordsAsync();
        pickedRecords.AddRange(restoredPicks);
    }

    // Point the detail pane at an email: park whatever was drafted on the previous one, set the
    // selection and reset the forms — then, if this email itself was parked earlier, put its
    // work back. Loading the body and thread is the caller's job — Select does both.
    private bool ApplySelection(MailboxMessage item)
    {
        ParkSelectedTriage();
        selected = item;
        // Pathway-first: pre-select the thread's own pathway when it already carries one (rendered as
        // a fixed badge); otherwise the triager chooses before any action beyond Discard is offered.
        pathway = TriagePathways.FromBucket(item.Bucket);
        actionError = null;
        ResetLinkState();
        composeOutcome = null;
        linkNote = null;
        poEmailNote = null;
        // The Outbox's own state deliberately survives here: queuedReplies and the open composer
        // anchor are workspace-level, not per-selection staging. Only the outcome banner clears.
        outboxNote = null;
        replyBody = "";
        replyToField = replyCcField = replyBccField = replySubject = "";
        replyShowBcc = false;
        replyOpen = false;
        replyIsForward = false;
        replyEnvelopePrefilled = false;
        replyAttachments = Array.Empty<ComposeDraftAttachment>();
        triageProjectId = "";
        projectAutoMatched = false;
        createTodoRows = new List<TodoDraftRow> { new() };
        stagedSystemActions.Clear();
        discardArmed = false;
        stagedCreate = null;
        createdNowRecords.Clear();
        relevantEventStaged = null;
        triageEntireThread = null;
        useThreadTags = null;
        // The document-triage ticks are drafted against ONE email's attachments — leaving them
        // across a selection change would send another email's attachment ids against this
        // message. Parked above, reset here, restored below like every other per-email draft.
        stagedDocControlIds.Clear();
        return RestoreParkedTriage(item.Id);
    }

    // Open the clicked email exactly as clicked — the selection stays on it; the thread panel
    // below still shows any newer replies for context. Actions apply to just this email unless
    // a thread-wide Yes on the "Entire thread" decision opts the apply into the whole conversation.
    private async Task Select(MailboxMessage item)
    {
        var restoredParkedWork = ApplySelection(item);
        // The email reads in the window OPPOSITE the list, side by side — desktop's version of
        // the old list/detail split, but with both halves loadable anywhere.
        workspace.ShowOpposite(PanelKind.Email, PanelKind.Inbox);
        // Body and thread are independent live reads — fetch them side by side.
        await Task.WhenAll(LoadDetailAsync(item), LoadThreadAsync(item));
        // A restored draft brings its record picks back, so the pool they came from is refetched.
        if (restoredParkedWork && !string.IsNullOrWhiteSpace(triageProjectId))
            await ReloadLinkRecordsKeepingPicksAsync();
        // Both reads have landed, so the whole chain is available to search for a project name
        // to pre-fill the pickers with.
        await TryPrefillProjectFromEmailAsync();
    }

    // Fetch the full body + attachment names on demand when an email is opened. Cancels any in-flight
    // fetch so rapid clicking can't race a stale result onto the newly selected email.
    private async Task LoadDetailAsync(MailboxMessage item)
    {
        detailCts?.Cancel();
        var cts = new CancellationTokenSource();
        detailCts = cts;

        detail = null;
        detailLoading = true;
        try
        {
            var loaded = await Intake.GetMessageDetailAsync(item.Id, item.InternetMessageId, cts.Token);
            if (!cts.IsCancellationRequested && selected?.Id == item.Id)
            {
                detail = loaded;
                PrefillReplyEnvelope(item, loaded);
                ReflectLiveTags(loaded);
            }
        }
        catch (OperationCanceledException) { /* superseded by a newer selection */ }
        catch { /* leave detail null so the view falls back to the preview */ }
        finally
        {
            if (selected?.Id == item.Id)
                detailLoading = false;
        }
    }

    // The open email's tags as the mailbox holds them NOW, carried on the detail read, replace the
    // copy the list page gave us: a row goes stale the moment something tags the email while it
    // stays open — System Actions' Create now raising a record from it (2026-08-25: the defect was
    // raised and the email tagged, but the Control Centre kept showing it untagged). The selected
    // record and its list row are both swapped, so the queue row grows its chips and the pane's
    // pathway follows the thread's real filing. A detail that couldn't read the tags changes nothing.
    private void ReflectLiveTags(MailboxMessageDetail loaded)
    {
        if (loaded.Categories is null || selected is null || selected.Id != loaded.MessageId) return;
        var hasSameTags = selected.Categories.SequenceEqual(loaded.Categories)
            && string.Equals(selected.Bucket, loaded.Bucket, StringComparison.OrdinalIgnoreCase);
        if (hasSameTags) return;

        var refreshed = selected with { Categories = loaded.Categories, Bucket = loaded.Bucket };
        selected = refreshed;
        if (refreshed.Bucket is not null) pathway = TriagePathways.FromBucket(refreshed.Bucket);
        items = ReplaceRow(items, refreshed);
        taggedItems = ReplaceRow(taggedItems, refreshed);
        discardedItems = ReplaceRow(discardedItems, refreshed);
        thread = ReplaceRow(thread, refreshed);
    }

    private static IReadOnlyList<MailboxMessage> ReplaceRow(IReadOnlyList<MailboxMessage> rows, MailboxMessage refreshed) =>
        rows.Any(row => row.Id == refreshed.Id)
            ? rows.Select(row => row.Id == refreshed.Id ? refreshed : row).ToList()
            : rows;

    // Re-read the open email's tags after an act that tagged it server-side while it stays selected
    // (Create now). Only the tags are refreshed — the body, thread and every draft on the email are
    // untouched, so nothing flickers and nothing staged is lost. Best-effort: a failed read leaves
    // the row as it was, and Apply's queue reload reconciles it anyway.
    private async Task RefreshSelectedTagsAsync(MailboxMessage anchor)
    {
        try
        {
            var loaded = await Intake.GetMessageDetailAsync(anchor.Id, anchor.InternetMessageId);
            ReflectLiveTags(loaded);
        }
        catch { /* the row stays as it was; the next queue reload shows the truth */ }
    }

    // Fetch the selected email's whole conversation for the thread panel. Same cancellation shape as
    // LoadDetailAsync: rapid clicking can't race a stale thread onto the newly selected email. The
    // list is cleared up front so a previous selection's thread never flashes against this one.
    private async Task LoadThreadAsync(MailboxMessage item)
    {
        threadCts?.Cancel();
        var cts = new CancellationTokenSource();
        threadCts = cts;

        thread = Array.Empty<MailboxMessage>();
        threadMatchedBySubject = false;
        threadError = null;
        if (string.IsNullOrEmpty(item.ConversationId)) { threadLoading = false; return; }

        threadLoading = true;
        try
        {
            var page = await Intake.ListConversationLiveAsync(item.ConversationId, item.Subject, cts.Token);
            if (cts.IsCancellationRequested || selected?.Id != item.Id)
                return;
            thread = page.Items;
            threadMatchedBySubject = page.MatchedBySubject;
        }
        catch (OperationCanceledException) { /* superseded by a newer selection */ }
        catch
        {
            if (selected?.Id == item.Id)
                threadError = "Couldn't read this email's thread — the conversation may still have replies.";
        }
        finally
        {
            if (selected?.Id == item.Id)
                threadLoading = false;
        }
    }

}
