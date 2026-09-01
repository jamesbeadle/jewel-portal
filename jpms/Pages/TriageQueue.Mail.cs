using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;

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
        outboxForwardTo = null;
        workspace.ShowOpposite(PanelKind.Outbox, anchor);
    }

    // Recipients a forward opens with — set by "Forward to QS", cleared by every other forward.
    private string? outboxForwardTo;

    private void StartForwardToQs()
    {
        if (selected is null) return;
        StartOutboxForward(selected, PanelKind.Client);
        outboxForwardTo = string.Join("; ", QsRecipients.Select(person => person.Email));
    }

    // "Edit in Email window" on the Outbox's current-reply row — that composer lives under the
    // open email, so open its section and show the Email window beside the Outbox.
    private void ShowCurrentReplyComposer()
    {
        replyOpen = true;
        workspace.ShowOpposite(PanelKind.Email, PanelKind.Outbox);
    }

    // The staged work + armed discard. The System Tags pane's tab mirrors the page's own
    // `pathway` field (the pathway decision); stagedCreate is the pane's drafted new record
    // (null = none) — StagedRecordKind decides whether Apply raises a request, a bid package, a work order or a defect.
    private bool discardArmed;
    private StagedRecordCreate? stagedCreate;
    // The "Relevant Event for Programme" decision — staged like everything else, applied by the
    // one Apply. Lives OUTSIDE System Tags because the programme bucket isn't a record anyone
    // picks or creates: every project has exactly one, so filing to it is a yes/no, not a search.
    // Nullable on purpose: null = not yet answered (the Yes/No pair renders blank), and Apply
    // refuses to run until the triager picks a side — a conscious decision, never a default.
    private bool? relevantEventStaged;
    // The "Entire thread" decision: Yes means every action in the apply spreads across the whole
    // current conversation (LinkThreadScope.EntireThread); No means each action tags only the
    // clicked email (MessageOnly). Nullable like the Relevant Event decision above — blank until
    // answered, required before Apply. Never persisted, cleared back to blank with the rest of
    // the staging on every selection/view change and after every apply.
    private bool? triageEntireThread;
    // The "Use existing tags" decision, offered only when the open email's thread ALREADY carries
    // record tags (the queue row's outline "Thread:" chips). Yes means Apply files this email
    // under those same records — the stems resolve back to records (ResolveRecordTags, the same
    // resolver behind the search chips) and each links exactly like a picked record — so a reply
    // to an already-linked thread is triaged in one answer, with nothing new to pick. No means
    // the triager picks this email's records themselves. Nullable like the two decisions above —
    // blank until answered, required before Apply whenever the row is on show.
    private bool? useThreadTags;

    // What the Subcontractor Communications browser tags against: the open QUEUE email (the
    // Tagged view manages its tags from the email pane instead), and the triage bar's project —
    // by name, because record-less communication tags carry no project to filter on.
    private string OpenQueueEmailSubject =>
        view == QueueView.Active && selected is not null ? selected.Subject : "";

    private string TriageProjectName =>
        AllProjects.FirstOrDefault(project => project.ProjectId == triageProjectId)?.Name ?? "";

    // Staging from a pathway pane IS the pathway decision (as the old System Tags tab switch
    // was) — parse the pane's label back onto the page's own pathway state so filing, to-dos and
    // a record-less reply all read one field.
    private void OnPathwayEngaged(string paneLabel)
    {
        if (Enum.TryParse<TriagePathway>(paneLabel, out var next)) SetPathway(next);
    }


    // Each pathway icon's badge = the staged work that pane owns: its record picks and category
    // ticks, its own staged actions, the drafted new record and the drafted to-dos. Every action
    // kind lives on exactly one pane (no shared "General" group — 2026-08-27 review), so the
    // kind→pane map is the pane configs themselves.
    private int PathwayBadge(PathwayPaneConfig config) =>
        pickedRecords.Count(record => config.LinkTypes.Contains(record.Type)
            || (config.Family is { } family && family.All.Any(familyRecord => familyRecord.RecordId == record.RecordId)))
        // Kinds can be offered on more than one pane (directory contact: Subcontractor,
        // Supplier, Internal), so staged actions count where they were STAGED, not by kind.
        + stagedSystemActions.Count(action => action.Pathway is { } stagedFrom
            ? stagedFrom == config.Pathway
            : config.AllActionKinds.Contains(action.Kind))
        + (config.Pathway == "Internal" ? CurrentTodoDrafts().Count : 0)
        + (StagedCreateReady && StagedCreatePathway(stagedCreate!.Kind) == config.Pathway ? 1 : 0);

    // Which pane's badge a drafted record counts on — mirrors which pane offers its create.
    private static string? StagedCreatePathway(StagedRecordKind kind) => kind switch
    {
        StagedRecordKind.Request or StagedRecordKind.TenderEnquiry
            or StagedRecordKind.BuildingControlInspection => "Client",
        StagedRecordKind.BidPackage or StagedRecordKind.WorkOrder or StagedRecordKind.Defect => "Subcontractor",
        StagedRecordKind.Inventory => "Supplier",
        StagedRecordKind.CalendarEvent => "Internal", // raised from the Internal pane, beside the Calendar
        _ => null
    };

    private bool StagedCreateReady => stagedCreate is { } sc && sc.IsReady;

    // A tender enquiry usually brings its own Lead project, so it needs no project in the bar.
    private bool StagedCreatesOwnProject =>
        stagedCreate is { Kind: StagedRecordKind.TenderEnquiry } sc && sc.TenderEnquiry.CreatesNewProject;

    // Joining an existing project is only ever the same job's second email — the bar's project
    // must itself still be a Lead.
    private bool TriageProjectIsLead =>
        !string.IsNullOrWhiteSpace(triageProjectId) && Projects.Find(triageProjectId)?.Stage == ProjectStage.Lead;

    private string? StagedTenderEnquiryProblem =>
        stagedCreate is { Kind: StagedRecordKind.TenderEnquiry } sc
            ? sc.TenderEnquiry.Problem(TriageProjectIsLead)
            : null;

    private string? StagedCalendarEventProblem =>
        stagedCreate is { Kind: StagedRecordKind.CalendarEvent } stagedEvent
            ? stagedEvent.CalendarEvent.Problem
            : null;

    private string? StagedBuildingControlInspectionProblem =>
        stagedCreate is { Kind: StagedRecordKind.BuildingControlInspection } stagedInspection
            ? stagedInspection.BuildingControlInspection.Problem
            : null;

    private string? TodoProjectNote =>
        string.IsNullOrWhiteSpace(triageProjectId)
            ? "No project set on the email — these will be company-wide items. Set the Project in the triage bar above to put them on a project's To-do tab."
            : $"Items land on the To-do tab of {ProjectLabelFor(triageProjectId)} — the email's project, set in the triage bar above.";

    // The assignee picker's option pool: the ROLES a to-do can be assigned to
    // (TodoRoles.AssignableAsTodoAssignee, served by ListTodoAssignableRoles) and, under each
    // role, the directory holders it can be pinned to (ListTodoAssignablePeople) — fetched once
    // when the page loads, shaped by TodoAssigneePicker.BuildOptions and shared by every to-do
    // draft row. Assignment is picker-only.
    private IReadOnlyList<SearchSelect.Option> todoAssigneeOptions = Array.Empty<SearchSelect.Option>();
    private IReadOnlyList<TodoAssignablePerson> assignablePeople = Array.Empty<TodoAssignablePerson>();

    // "Forward to QS" (2026-08-22): everyone in the staff directory with the QS role.
    private IReadOnlyList<TodoAssignablePerson> QsRecipients =>
        assignablePeople.Where(person => person.Role == Role.QuantitySurveyor).ToList();

    // The drafts exactly as they will be posted. Built in one place so the count promised on the
    // summary and the batch the apply actually sends can never disagree.
    private List<TodoItemDraft> CurrentTodoDrafts() => createTodoRows
        .Where(row => !string.IsNullOrWhiteSpace(row.Title))
        .Select(row => new TodoItemDraft(
            row.Title.Trim(),
            NullIfBlank(row.Notes),
            ParseTodoAssignees(row.Assignees),
            ParseDate(row.Due)))
        .ToList();

    private async Task LoadTodoAssignableRolesAsync()
    {
        // A failed load leaves the picker with no options rather than blocking triage — to-dos can
        // still be created, they just go in unassigned.
        try
        {
            var rolesTask = Todos.ListAssignableRolesAsync();
            var peopleTask = Todos.ListAssignablePeopleAsync();
            assignablePeople = await peopleTask;
            todoAssigneeOptions = TodoAssigneePicker.BuildOptions(await rolesTask, assignablePeople);
            await StageFilter.EnsureLoadedAsync();
            StageFilter.OnChange += StageFilterChanged;
        }
        catch { }
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        RequestRegister.OnChange += StateHasChanged;
        workspace.OnChange += StateHasChanged;
        // The sort preference has to land first — LoadAsync pages the mailbox in this order, so
        // reading it late would fetch the first page the wrong way round. It is a local-storage
        // read, so it costs almost nothing.
        newestFirst = await SortStorage.ReadNewestFirstAsync(Auth.CurrentUser!.Email);
        sessionReady = true;
        // Paint the chrome before the four fetches: Blazor re-renders OnInitializedAsync only at
        // its FIRST await, which has already passed. The sort toggle is drawn from newestFirst, so
        // it goes out with the rest rather than flipping under the cursor a moment later.
        StateHasChanged();

        // The remaining four are independent of each other; issued together the page waits once
        // for the slowest instead of for the sum. Triage is the heaviest page in the app to open.
        //
        // Every one of them has to be non-throwing. Task.WhenAll rethrows the first failure, and an
        // exception escaping OnInitializedAsync takes the whole page down to the error boundary —
        // which is exactly what a failing project list used to do here, turning one bad read into a
        // dead triage queue. The other three already swallowed their own failures; the project list
        // was the odd one out. The error still reaches the toast, because the query client reports
        // it to the error sink before rethrowing, so nothing is hidden by catching it here.
        await Task.WhenAll(
            LoadProjectsAsync(),
            LoadAsync(),
            LoadUnassignedAsync(),
            LoadTodoAssignableRolesAsync(),
            LoadRecentTriageAsync());

        // A finder elsewhere (the to-do searches' email results) may have sent one specific email
        // here to be opened — select THAT, on the pile it lives in, instead of the default landing.
        if (OpenEmail.Take() is MailboxMessage handedOver)
        {
            if (handedOver.Categories.Count > 0 || handedOver.Bucket is not null)
                await SwitchView(QueueView.Tagged);
            await Select(handedOver);
            return;
        }

        // Land straight in the first email: opening the top of the queue is what a triager does
        // first every time, so the page does it for them. Initial load only — after an action the
        // deliberately-cleared pane is where the outcome banners (reply draft, partial link) show.
        if (view != QueueView.Active || selected is not null || items.Count == 0) return;
        await Select(items[0]);
    }

    // The project list only feeds the "file this email against a project" pickers. Losing it should
    // cost the pickers their options, not cost the triager the entire queue.
    private async Task LoadProjectsAsync()
    {
        try { await Projects.RefreshAsync(CancellationToken.None); }
        catch { /* reported by the query client; the pickers render empty */ }
    }

    private async Task SwitchView(QueueView next)
    {
        if (view == next) return;
        ParkSelectedTriage();
        view = next;
        selected = null;
        detail = null;
        detailLoading = false;
        discardArmed = false;
        stagedCreate = null;
        relevantEventStaged = null;
        triageEntireThread = null;
        useThreadTags = null;
        pickedRecords.Clear();
        actionError = null;
        composeOutcome = null;
        if (next == QueueView.Discarded) { ResetDiscardedPaging(); await LoadDiscardedAsync(); }
        else if (next == QueueView.Tagged)
        {
            selectedTags.Clear(); pathwayBucketFilter = null; filterOpen = false;
            // Entering the tab starts from the unfiltered pile — the search resets with the rest.
            taggedSearchDebounce?.Cancel();
            taggedSearch = ""; taggedSearchPending = ""; taggedSearching = false;
            taggedSearchRecord = null; taggedSearchTag = null; taggedSearchResults = null;
            ResetTaggedPaging(); await LoadTaggedAsync();
        }
        else { ResetQueuePaging(); await Task.WhenAll(LoadAsync(), LoadRecentTriageAsync()); }
    }

    // Flip the sort order, remember the choice, and re-read the visible list from page one (an
    // offset cursor from one order is meaningless in the other). Selection is cleared like a view
    // switch: the previously open email may not even be on the new first page.
    private async Task SetSortAsync(bool newest)
    {
        if (newestFirst == newest) return;
        newestFirst = newest;
        if (Auth.CurrentUser is not null)
            await SortStorage.WriteAsync(Auth.CurrentUser.Email, newest);
        ParkSelectedTriage();
        selected = null;
        detail = null;
        detailLoading = false;
        discardArmed = false;
        stagedCreate = null;
        relevantEventStaged = null;
        triageEntireThread = null;
        useThreadTags = null;
        pickedRecords.Clear();
        actionError = null;
        composeOutcome = null;
        if (view == QueueView.Discarded) { ResetDiscardedPaging(); await LoadDiscardedAsync(); }
        else if (view == QueueView.Tagged) { ResetTaggedPaging(); await LoadTaggedAsync(); }
        else { ResetQueuePaging(); await LoadAsync(); }
    }

    private void ResetQueuePaging() { queueCursors = new() { null }; queueIndex = 0; queueNext = null; }
    private void ResetDiscardedPaging() { discardedCursors = new() { null }; discardedIndex = 0; discardedNext = null; }
    private void ResetTaggedPaging() { taggedCursors = new() { null }; taggedIndex = 0; taggedNext = null; }

    // After an action consumes an email, its list shrinks under the pager. The cursor is a plain
    // offset (see MailboxGraphClient.ListFilteredAsync), so re-reading the SAME page simply refills
    // it from the emails further down — the triager stays on the page they were working, and emails
    // they deliberately skipped stay behind on earlier pages instead of being re-presented from
    // page one after every action. Only when the page has fallen off the end entirely (the last
    // email on the last page was consumed) does this step back — one page at a time, never past
    // page one. A failed reload keeps the index rather than guessing: loadError already tells the
    // story, and Previous/Next still work. View switches and sort flips still reset to page one —
    // those genuinely start a new read of the list.
    private async Task ReloadQueueInPlaceAsync()
    {
        await LoadAsync();
        while (loadError is null && items.Count == 0 && queueIndex > 0)
        {
            queueIndex--;
            await LoadAsync();
        }
    }

    private async Task ReloadDiscardedInPlaceAsync()
    {
        await LoadDiscardedAsync();
        while (loadError is null && discardedItems.Count == 0 && discardedIndex > 0)
        {
            discardedIndex--;
            await LoadDiscardedAsync();
        }
    }

    private async Task ReloadTaggedInPlaceAsync()
    {
        await LoadTaggedAsync();
        while (loadError is null && taggedItems.Count == 0 && taggedIndex > 0)
        {
            taggedIndex--;
            await LoadTaggedAsync();
        }
    }

    private async Task LoadAsync()
    {
        loadError = null;
        listLoading = true;
        // Paint the spinner before the fetch: Blazor only re-renders an event handler at its FIRST
        // await, so when a caller awaited something else first (SetSortAsync persists the choice to
        // localStorage before reloading), setting listLoading here would otherwise never render and
        // the list sits still until the new page lands.
        StateHasChanged();
        try
        {
            var result = await Intake.ListInboxLiveAsync(queueCursors[queueIndex], PageSize, newestFirst);
            items = result.Items;
            total = result.Total;
            queueNext = result.NextCursor;
            // Record the cursor for the next page so Next can advance to it.
            if (queueNext is not null && queueIndex == queueCursors.Count - 1)
                queueCursors.Add(queueNext);
        }
        catch
        {
            loadError = "Couldn't load the inbox. Please try again.";
            items = Array.Empty<MailboxMessage>();
            total = 0;
            queueNext = null;
        }
        finally
        {
            listLoading = false;
            queueArrived = true;
        }
    }

    private async Task LoadDiscardedAsync()
    {
        loadError = null;
        listLoading = true;
        StateHasChanged(); // paint the spinner before the fetch — see LoadAsync
        try
        {
            var result = await Intake.ListDiscardedLiveAsync(discardedCursors[discardedIndex], PageSize, newestFirst);
            discardedItems = result.Items;
            discardedTotal = result.Total;
            discardedNext = result.NextCursor;
            if (discardedNext is not null && discardedIndex == discardedCursors.Count - 1)
                discardedCursors.Add(discardedNext);
        }
        catch
        {
            loadError = "Couldn't load discarded emails. Please try again.";
            discardedItems = Array.Empty<MailboxMessage>();
            discardedTotal = 0;
            discardedNext = null;
        }
        finally
        {
            listLoading = false;
            discardedArrived = true;
        }
    }

    private async Task PreviousPage()
    {
        if (queueIndex <= 0) return;
        queueIndex--;
        await LoadAsync();
    }

    private async Task NextPage()
    {
        if (queueNext is null) return;
        queueIndex++;
        await LoadAsync();
    }

    private async Task PreviousDiscarded()
    {
        if (discardedIndex <= 0) return;
        discardedIndex--;
        await LoadDiscardedAsync();
    }

    private async Task NextDiscarded()
    {
        if (discardedNext is null) return;
        discardedIndex++;
        await LoadDiscardedAsync();
    }

    private async Task LoadTaggedAsync()
    {
        loadError = null;
        listLoading = true;
        StateHasChanged(); // paint the spinner before the fetch — see LoadAsync
        try
        {
            // The search's resolved record tag, the pathway chip and the record-tag multi-select
            // are mutually exclusive (see the pathwayBucketFilter note), so exactly one of them
            // feeds the server's tags filter — the search first, since the others render disabled
            // while it is live.
            var filter = taggedSearchTag is not null
                ? new List<string> { taggedSearchTag }
                : pathwayBucketFilter is not null
                    ? new List<string> { pathwayBucketFilter }
                    : selectedTags.Count == 0 ? null : selectedTags.ToList();
            var result = await Intake.ListTaggedLiveAsync(taggedCursors[taggedIndex], PageSize, filter, newestFirst);
            taggedItems = result.Items;
            taggedTotal = result.Total;
            taggedNext = result.NextCursor;
            if (taggedNext is not null && taggedIndex == taggedCursors.Count - 1)
                taggedCursors.Add(taggedNext);
            // Remember every tag we see so the filter dropdown can offer them.
            foreach (var message in taggedItems)
                foreach (var tag in message.Categories)
                    knownTags.Add(tag);
        }
        catch
        {
            loadError = "Couldn't load tagged emails. Please try again.";
            taggedItems = Array.Empty<MailboxMessage>();
            taggedTotal = 0;
            taggedNext = null;
        }
        finally
        {
            listLoading = false;
            taggedArrived = true;
        }
    }

    private void ToggleFilterMenu() => filterOpen = !filterOpen;

    // ---- Reply compose (send for real — decision 2026-08-04) ----

    // Reply-all prefill, computed once per selection from the opened email's envelope: the sender
    // (or their Reply-To) goes in To; the original To + Cc — minus whoever is now in To — go in Cc.
    // The projects mailbox itself is filtered out — Cc'ing it would deliver a copy back to the
    // Inbox and land it in the triage queue (decision 2026-08-07: no auto-Cc anywhere).
    private void PrefillReplyEnvelope(MailboxMessage item, MailboxMessageDetail loaded)
    {
        if (replyEnvelopePrefilled) return;
        // A forward's envelope is deliberately blank (FW subject already set) — the late-landing
        // detail must not overwrite it with the reply-all prefill.
        if (replyIsForward) return;

        var toAddress = loaded.ReplyTo ?? loaded.FromEmail ?? item.FromEmail;
        replyToField = toAddress ?? "";

        var ccAddresses = (loaded.To ?? Array.Empty<string>())
            .Concat(loaded.Cc ?? Array.Empty<string>())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Where(a => !a.Equals(toAddress, StringComparison.OrdinalIgnoreCase))
            // Strip the projects mailbox from the prefill: replying with it on Cc would deliver
            // the sent email back into the Inbox, where it lands in the triage queue again.
            .Where(a => loaded.MailboxAddress is null || !a.Equals(loaded.MailboxAddress, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        replyCcField = string.Join("; ", ccAddresses);

        var subject = loaded.Subject ?? item.Subject;
        replySubject = string.IsNullOrWhiteSpace(subject) ? "RE: (no subject)"
            : subject.TrimStart().StartsWith("RE:", StringComparison.OrdinalIgnoreCase) ? subject.Trim()
            : $"RE: {subject.Trim()}";

        replyEnvelopePrefilled = true;
    }

    private bool ReplyIsSendable =>
        ParseRecipients(replyToField).Count > 0
        && !string.IsNullOrWhiteSpace(replySubject)
        && HtmlHasContent(replyBody);

    // True while a reply is drafted but unsent — the filing panes' own buttons stand down so a
    // written reply can't be silently left behind by a tag-only action; Send applies both.
    private bool ReplyDraftPending => replyOpen && HtmlHasContent(replyBody);

    // The email's global project, giving the attachment picker its context.
    private string ComposeContextProjectId => triageProjectId;

    // What Send will do besides sending, phrased for the note above the button. Null = nothing.
    // Everything the action bar will do, phrased as one sentence ("send your reply, raise 2
    // to-dos and link this email to the selected record"). Null = nothing pending, button disabled.
    private string? PendingSummary
    {
        get
        {
            var parts = new List<string>();
            if (ReplyDraftPending)
                parts.Add(replyIsForward ? "send your forward" : "send your reply");
            if (queuedReplies.Count > 0)
                parts.Add(queuedReplies.Count == 1
                    ? $"send the lined-up {(queuedReplies[0].IsForward ? "forward" : "reply")} to {queuedReplies[0].AnchorFrom}"
                    : $"send {queuedReplies.Count} lined-up emails");
            var todoCount = CurrentTodoDrafts().Count;
            if (todoCount > 0)
                parts.Add(todoCount == 1 ? "raise the to-do" : $"raise {todoCount} to-dos");
            if (pickedRecords.Count > 0)
                parts.Add(pickedRecords.Count == 1
                    ? $"link this email to {pickedRecords[0].Reference}"
                    : $"link this email to {string.Join(", ", pickedRecords.Take(3).Select(r => r.Reference))}{(pickedRecords.Count > 3 ? $" +{pickedRecords.Count - 3} more" : "")}");
            if (useThreadTags == true && SelectedThreadTags is { Count: > 0 } inheritStems)
                parts.Add(inheritStems.Count == 1
                    ? $"file it under the thread's existing tag ({TagLabel(inheritStems[0])})"
                    : $"file it under the thread's existing tags ({string.Join(", ", inheritStems.Take(3).Select(TagLabel))}{(inheritStems.Count > 3 ? $" +{inheritStems.Count - 3} more" : "")})");
            if (stagedDocControlIds.Count > 0)
                parts.Add(stagedDocControlIds.Count == 1
                    ? "send 1 attachment to Document Triage"
                    : $"send {stagedDocControlIds.Count} attachments to Document Triage");
            if (StagedCreateReady && (!string.IsNullOrWhiteSpace(triageProjectId) || StagedCreatesOwnProject))
                parts.Add(stagedCreate!.Kind switch
                {
                    StagedRecordKind.BidPackage => "create the bid package from this email",
                    StagedRecordKind.TenderEnquiry => StagedCreatesOwnProject
                        ? "create a Lead project and log the tender enquiry from this email"
                        : "log the tender enquiry from this email",
                    StagedRecordKind.WorkOrder => StagedWorkOrderSummary(stagedCreate),
                    StagedRecordKind.Defect => "raise the defect from this email",
                    StagedRecordKind.Inventory => "add the inventory item from this email",
                    StagedRecordKind.CalendarEvent => "raise the calendar event from this email",
                    StagedRecordKind.BuildingControlInspection => "raise the building control inspection from this email",
                    _ => stagedCreate.RequestKind == RequestType.Rfi
                        ? "raise the RFI from this email"
                        : "create the request from this email"
                });
            if (stagedSystemActions.Count == 1)
                parts.Add($"run 1 system action ({stagedSystemActions[0].Summary})");
            else if (stagedSystemActions.Count > 1)
                parts.Add($"run {stagedSystemActions.Count} system actions");
            if (relevantEventStaged == true)
                parts.Add("tag it a Relevant Event for the Programme");
            if (discardArmed)
                parts.Add("discard this email and its thread");
            // Create now already raised the record and tagged the email — with nothing else
            // staged, the apply's one remaining job is clearing the dealt-with email from the
            // queue. Without this clause Apply sat disabled after a create-now-only triage
            // ("it expects a staged tag") and the email was stuck open (reported 2026-08-28).
            if (parts.Count == 0 && selected is not null && createdNowRecords.Count > 0)
                return createdNowRecords.Count == 1
                    ? $"clear this email from the queue — {createdNowRecords[0].Reference} is already raised and the email tagged to it"
                    : $"clear this email from the queue — {string.Join(", ", createdNowRecords.Select(r => r.Reference))} are already raised and the email tagged to them";
            if (parts.Count == 0) return null;
            var summary = parts.Count == 1 ? parts[0] : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];
            // A thread-wide Yes changes what an apply MEANS, so the sentence says so — but only when
            // something staged actually spreads (a bare discard is thread-wide regardless).
            if (triageEntireThread == true && !(discardArmed && parts.Count == 1))
                summary += " — covering every email currently in this thread";
            // Lined-up replies inherit the triage's record picks: the sentence says so, because
            // tagging OTHER emails is the one effect a reader wouldn't otherwise expect.
            if (queuedReplies.Count > 0 && pickedRecords.Count > 0)
                summary += $" (the lined-up {(queuedReplies.Count == 1 ? "email's anchor is" : "emails' anchors are")} tagged to the picked records too)";
            return summary;
        }
    }

    // The staged work order phrased for the apply note, counting the record-keeping attachments
    // (ticked email files + picked uploads) it will keep on the new order.
    private static string StagedWorkOrderSummary(StagedRecordCreate staged)
    {
        var label = staged.SaveAsDraft
            ? "raise the draft work order from this email"
            : "raise the work order from this email and email the purchase order to the subcontractor";
        var attachmentCount = staged.EmailAttachmentIds.Count + staged.UploadFiles.Count;
        return attachmentCount == 0
            ? label
            : $"{label} (keeping {attachmentCount} attachment{(attachmentCount == 1 ? "" : "s")} on the order — not emailed)";
    }

    // Done on a pathway pane: confirm the picks and land that window back on the open
    // email — the same place every time. The plain pane-history fallback ("whatever this window
    // showed before") read as a bug in practice: with System Actions earlier in the history,
    // Done appeared to open the RFI form out of nowhere (reported 2026-08-20). Close() first so
    // SystemTags leaves the history entirely — closing the email later must not resurface a
    // confirmed tags window and silently re-block Apply. When the email is already on show in
    // the other window, the plain close is enough — no point opening a mirror copy over here.
    private void ClosePathwayPane(PanelKind pane)
    {
        var side = workspace.SideShowing(pane);
        workspace.Close(pane);
        if (side is not { } paneSide || selected is null) return;
        // On mobile only the left pane is on screen, so the right pane "showing" the email
        // doesn't count as the email being visible — bring it to the one real window.
        var emailVisible = workspace.IsDesktop && workspace.SideShowing(PanelKind.Email) is not null;
        if (!emailVisible) workspace.Show(PanelKind.Email, paneSide);
    }

    // An action just closed the open email (applied, discarded, restored, re-tagged), so the
    // email window and its reading copy have nothing left to show. Bring the queue list back on
    // show wherever the panes were left — without this, an apply run while the mirror covered
    // the inbox landed on two empty windows with the list nowhere in sight (reported
    // 2026-08-28: "loaded without the mailbox selected"). The mirror closes outright (a reading
    // copy of nothing has no reason to wait in the history); the inbox then either resurfaces
    // from that pane's own history or is shown on the left, its home side.
    private void ReturnWorkspaceToQueue()
    {
        workspace.Close(PanelKind.EmailMirror);
        if (workspace.SideShowing(PanelKind.Inbox) is null)
            workspace.Show(PanelKind.Inbox, PanelSide.Left);
    }

    // NOTE (2026-08-27): the old "Apply stands down while a tags window is open" rule is GONE.
    // It fit the one modal System Tags pane; with four standing pathway panes (which also host
    // browsable registers) it left Apply disabled almost permanently, with the reason buried in
    // a tooltip — Nigel filled everything in, pressed Done everywhere he could see, and still
    // couldn't apply. Picks and ticks stage LIVE into the page's one list, and every staged
    // record form is readiness-checked by DoApplyAll itself, so an open pane holds nothing back.

    // True while either of the bar's Yes/No pairs is still blank for the open email. Apply (and
    // save-as-drafts) stand down until both are answered — the pairs deliberately start with
    // NEITHER side picked, so tagging the programme and sweeping the thread are always decisions
    // someone actually made, never a default that slipped through.
    private bool TriageDecisionsMissing =>
        selected is not null && MissingDecisionNames().Count > 0;

    // The blank pairs still awaiting an answer, by their on-screen names — one list feeds both
    // the amber hint next to Apply and the belt-and-braces error inside DoApplyAll, so the two
    // can never drift. "Use existing tags" counts only while its row is on show (the thread
    // actually carries tags to inherit).
    private List<string> MissingDecisionNames()
    {
        var missing = new List<string>();
        if (relevantEventStaged is null) missing.Add("Relevant Event for Programme");
        if (triageEntireThread is null) missing.Add("Entire thread");
        if (SelectedThreadTags.Count > 0 && useThreadTags is null) missing.Add("Use existing tags");
        return missing;
    }

    private static string AndJoin(IReadOnlyList<string> parts) =>
        parts.Count <= 1
            ? parts.FirstOrDefault() ?? ""
            : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[^1];

    // The record tags the open email's thread already carries — the queue row's outline "Thread:"
    // chips, populated only on queue listings (a new reply to an already-linked thread). Empty
    // everywhere else, so the "Use existing tags" row and its gate simply don't exist there.
    private IReadOnlyList<string> SelectedThreadTags =>
        selected?.ThreadTags is { Count: > 0 } tags ? tags : Array.Empty<string>();

    // True while attachments are ticked for Document Triage but the email has no project.
    // The project is REQUIRED for a Document Triage send (decision 2026-08-28): a file landing
    // in the queue with no project is as good as discarded, and the triage bar — where the
    // email says which job it is — is the cheapest place to set it. Same standing-hint
    // treatment as the Yes/No pairs (2026-08-27: the disable reason stands next to the button).
    private bool DocTriageProjectMissing =>
        selected is not null && stagedDocControlIds.Count > 0 && string.IsNullOrWhiteSpace(triageProjectId);

    private const string DocTriageProjectMissingHint =
        "Set the Project first — attachments can't go to Document Triage without one";

    private string DecisionsMissingHint =>
        $"Answer {AndJoin(MissingDecisionNames())} — Yes or No — first";

    // The bar's Yes/No pair: two joined pill halves, neither lit until the triager picks a side
    // (null = blank). Picked reads like a picked record row (accent border on raised surface);
    // the unpicked side stays muted. Clicking the picked side again is a no-op, not a clear —
    // the whole point is that "no answer" isn't a state anyone can put back.
    private static string YesNoClass(bool? decided, bool answer, bool first) =>
        "px-2.5 py-1 text-xs border transition "
        + (first ? "rounded-l-lg" : "rounded-r-lg -ml-px")
        + (decided == answer
            ? " relative border-accent bg-surface-raised text-content font-medium"
            : " border-line text-content-subtle hover:text-content hover:border-line-strong");

    private string ApplyButtonLabel
    {
        get
        {
            var filing = CurrentTodoDrafts().Count > 0
                || pickedRecords.Count > 0
                || (useThreadTags == true && SelectedThreadTags.Count > 0)
                || relevantEventStaged == true
                || stagedSystemActions.Count > 0
                || stagedDocControlIds.Count > 0
                || (StagedCreateReady && (!string.IsNullOrWhiteSpace(triageProjectId) || StagedCreatesOwnProject));
            var sendCount = (ReplyDraftPending ? 1 : 0) + queuedReplies.Count;
            if (sendCount > 0)
            {
                var send = sendCount == 1 ? "Send reply" : $"Send {sendCount} replies";
                return filing ? $"{send} & file" : send;
            }
            return discardArmed && !filing ? "Discard email" : "Apply";
        }
    }

    // Open the composer under the open email as a reply or a forward. The two kinds prime the
    // envelope differently — reply-all prefill vs a blank envelope with a "FW:" subject — so
    // switching kind re-primes it (the written body and any extra attachments survive; original
    // attachments picked for a reply are dropped on a switch to forward, because Graph carries
    // the originals on a forward draft automatically).
    private void OpenReplyComposer(bool forward)
    {
        replyOpen = true;
        if (replyIsForward == forward) return;
        replyIsForward = forward;
        replyShowBcc = false;
        if (forward)
        {
            replyToField = replyCcField = replyBccField = "";
            replySubject = MailCompose.ForwardSubjectFor(detail?.Subject ?? selected?.Subject);
            replyAttachments = replyAttachments
                .Where(a => a.Source != ComposeAttachmentSource.OriginalMessage)
                .ToList();
        }
        else
        {
            replyToField = replyCcField = replyBccField = "";
            replySubject = "";
            replyEnvelopePrefilled = false;
            if (selected is { } item && detail is { } loaded) PrefillReplyEnvelope(item, loaded);
        }
    }

    private void DiscardReplyDraft()
    {
        replyOpen = false;
        replyBody = "";
        replyAttachments = Array.Empty<ComposeDraftAttachment>();
        // A discarded forward hands the composer back in reply shape, reply-all re-prefilled, so
        // the next "↩ Reply" press starts from the normal envelope.
        if (replyIsForward)
        {
            replyIsForward = false;
            replyToField = replyCcField = replyBccField = "";
            replySubject = "";
            replyEnvelopePrefilled = false;
            if (selected is { } item && detail is { } loaded) PrefillReplyEnvelope(item, loaded);
        }
    }

    // The shared composer rules (MailCompose), aliased so every call site here reads the same as
    // it always did — the logic itself is defined once for all mail-writing surfaces.
    private static bool HtmlHasContent(string html) => MailCompose.HtmlHasContent(html);

    private void OnReplyAttachmentsChanged(IReadOnlyList<ComposeDraftAttachment> attachments) =>
        replyAttachments = attachments;

    private static IReadOnlyList<(string PartName, Microsoft.AspNetCore.Components.Forms.IBrowserFile File)> UploadPartsOf(
        IReadOnlyList<ComposeDraftAttachment> attachments) => MailCompose.UploadPartsOf(attachments);

    private static List<ComposeRecipient> ParseRecipients(string field) => MailCompose.ParseRecipients(field);

    // ONE Send: applies whatever filing is set up in the panes above (record links, a new
    // record, to-dos), then sends the reply. Filing and replying are two halves of dealing with
    // an email — deliberately combinable, never forced apart. Filing runs first (each command
    // verifies its tags before saving); the send comes last so a filing failure stops everything
    // with the email still queued, and a send failure leaves the thread filed with the reply
    // safe in Drafts (the outcome banner says exactly which).
    // THE action: applies everything the three sections have set up — the reply (section 1),
    // the to-do drafts (section 2) and the record filing (section 3) — in one click, in that
    // order. Filing runs first (every tag verified before anything saves); the send comes last so
    // a filing failure stops everything with the email still queued, and a send failure leaves the
    // thread filed with the reply safe in Drafts (the outcome banner says exactly which).
    private async Task DoApplyAll(bool saveAsDraftOnly)
    {
        if (busy) return;
        // Lined-up Outbox replies apply on their own — no selection needed (decision 2026-08-12).
        if (selected is null && queuedReplies.Count == 0) return;

        var anchorEmail = selected;
        var replying = ReplyDraftPending && anchorEmail is not null;
        var drafts = anchorEmail is null ? new List<TodoItemDraft>() : CurrentTodoDrafts();
        var picks = pickedRecords.ToList();
        var createReady = anchorEmail is not null && StagedCreateReady
            && (!string.IsNullOrWhiteSpace(triageProjectId) || StagedCreatesOwnProject);
        var relevantEvent = relevantEventStaged == true && anchorEmail is not null;
        var discarding = discardArmed && anchorEmail is not null;
        // "Use existing tags" answered Yes: the thread's tag stems, captured now so the apply
        // works from what the triager saw. Resolved to records inside the try — before anything
        // else lands — and linked exactly like picks.
        var inheritStems = useThreadTags == true && anchorEmail is not null
            ? SelectedThreadTags.ToList()
            : new List<string>();
        // One scope for the whole apply: a thread-wide Yes opts every staged action into the thread.
        var scope = triageEntireThread == true ? LinkThreadScope.EntireThread : LinkThreadScope.MessageOnly;
        // A create-now-only triage: the record is already raised and the email tagged to it
        // (Create now did both), so there is nothing left to RUN — but the apply still owns the
        // close-out below (queue reload, selection cleared). Letting it through is what
        // un-sticks the Apply button after Create now; every step in the body no-ops on its
        // own zero-count guard.
        var createdNowOnly = anchorEmail is not null && createdNowRecords.Count > 0;
        if (!replying && drafts.Count == 0 && picks.Count == 0 && !createReady && !relevantEvent && !discarding
            && stagedSystemActions.Count == 0 && queuedReplies.Count == 0 && stagedDocControlIds.Count == 0
            && inheritStems.Count == 0 && !createdNowOnly) return;

        // The bar's Yes/No pairs start blank on purpose — an apply with any unanswered is a
        // decision not yet made. Belt-and-braces behind the disabled button, so no other route
        // into the apply can land with a blank answer.
        if (anchorEmail is not null && MissingDecisionNames() is { Count: > 0 } missingDecisions)
        {
            actionError = $"Answer {AndJoin(missingDecisions)} — Yes or No — then Apply.";
            return;
        }

        // A half-built lined-up email is a decision not yet made — finish it or remove it, rather
        // than have Apply skip it (or the server reject it after the filing has already landed).
        if (queuedReplies.FirstOrDefault(lined => lined.Problem is not null) is { } notReady)
        {
            actionError = $"A lined-up {(notReady.IsForward ? "forward" : "reply")} ({notReady.AnchorSubject}) isn't ready — {notReady.Problem} Finish it in the Outbox, or remove it.";
            return;
        }

        // A reply and a discard contradict each other — an email worth answering isn't spam.
        // (Same rule for an unsent forward: send or discard the draft before binning the email.)
        if (discarding && replying)
        {
            actionError = $"Discard and a {(replyIsForward ? "forward" : "reply")} don't mix — send (or discard) the draft first.";
            return;
        }
        if (discarding && picks.Count > 0)
        {
            actionError = "Discard and record links don't mix — unpick the records first.";
            return;
        }
        if (discarding && inheritStems.Count > 0)
        {
            actionError = "Discard and the thread's existing tags don't mix — answer No to Use existing tags, or disarm the discard.";
            return;
        }
        if (discarding && relevantEvent)
        {
            actionError = "Discard and a Relevant Event tag don't mix — answer No to Relevant Event, or disarm the discard.";
            return;
        }
        // A Relevant Event answered Yes without a project is a decision not yet made — same rule as the
        // staged create: finish it or clear it, rather than have Apply quietly skip it.
        if (relevantEvent && string.IsNullOrWhiteSpace(triageProjectId))
        {
            actionError = "To tag a Relevant Event for the Programme, set the email's Project first — or answer No.";
            return;
        }

        // Attachments bound for Document Triage without a project are the same "decision not
        // yet made" (decision 2026-08-28): an unassigned file in the queue is as good as
        // discarded. Belt-and-braces behind the disabled button, like the Yes/No gate above.
        if (anchorEmail is not null && stagedDocControlIds.Count > 0 && string.IsNullOrWhiteSpace(triageProjectId))
        {
            actionError = "To send attachments to Document Triage, set the email's Project first — or untick them.";
            return;
        }

        // A staged new record without a project is a decision not yet made — finish it or clear
        // it, rather than have Apply quietly skip it.
        if (StagedCreateReady && !createReady)
        {
            actionError = "To create the record, set the email's Project first — or remove the staged record in the pathway pane's Actions.";
            return;
        }
        // A staged work order that isn't complete yet (no subcontractor, no priced line…) is the
        // same "decision not yet made": finish it or clear it, rather than let the server reject
        // a half-built order after the to-dos have already been raised.
        if (createReady && stagedCreate is { Kind: StagedRecordKind.WorkOrder } stagedOrder
            && stagedOrder.WorkOrderProblem is { } orderProblem)
        {
            actionError = $"The staged work order isn't ready — {orderProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        // Same "decision not yet made" rule for a staged defect with no description yet.
        if (createReady && stagedCreate is { Kind: StagedRecordKind.Defect } stagedDefect
            && stagedDefect.DefectProblem is { } defectProblem)
        {
            actionError = $"The staged defect isn't ready — {defectProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        // And for a staged inventory item with no product name yet.
        if (createReady && stagedCreate is { Kind: StagedRecordKind.Inventory } stagedInventory
            && stagedInventory.InventoryProblem is { } inventoryProblem)
        {
            actionError = $"The staged inventory item isn't ready — {inventoryProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        if (createReady && StagedTenderEnquiryProblem is { } enquiryProblem)
        {
            actionError = $"The staged tender enquiry isn't ready — {enquiryProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        // Same "decision not yet made" rule for a staged calendar event that isn't complete yet.
        if (createReady && StagedCalendarEventProblem is { } calendarProblem)
        {
            actionError = $"The staged calendar event isn't ready — {calendarProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        // And for a staged building control inspection.
        if (createReady && StagedBuildingControlInspectionProblem is { } inspectionProblem)
        {
            actionError = $"The staged inspection isn't ready — {inspectionProblem} Finish it in the pathway pane's Actions, or remove it.";
            return;
        }
        if (replying)
        {
            if (ParseRecipients(replyToField).Count == 0) { actionError = "Add a To recipient to the reply."; return; }
            if (string.IsNullOrWhiteSpace(replySubject)) { actionError = "Write a subject for the reply."; return; }
            // A reply alone triages the thread as Replied — pathway-less is fine (answering IS
            // dealing with it); choosing a tab in System Tags files it under that side as well.
        }

        var anchor = selected;
        var uploadParts = UploadPartsOf(replyAttachments);
        actionError = null;
        busy = true;
        try
        {
            var filed = false;

            // ---- "Use existing tags" answered Yes: resolve the thread's tag stems back to
            //      records FIRST (the same ResolveRecordTags behind the search chips), so a stem
            //      that no longer names anything stops the apply before anything else lands —
            //      the same every-tag-verified-before-anything-saves rule as the rest of the
            //      filing. The links themselves land with the picks below. ----
            IReadOnlyList<LinkableRecord> inheritedRecords = Array.Empty<LinkableRecord>();
            if (anchor is not null && inheritStems.Count > 0)
            {
                busyLabel = "Matching the thread's tags";
                inheritedRecords = await Queries.AskAsync(
                    new ResolveRecordTags(inheritStems.Select(TagLabel).ToList()), CancellationToken.None);
                if (inheritedRecords.Count == 0)
                {
                    actionError = "The thread's existing tags couldn't be matched to records — pick this email's records by hand instead.";
                    return;
                }
            }

            // ---- Document Triage: ticked attachments copy out FIRST, so the files are safely
            //      in the queue before anything else (a discard included) moves the email on.
            //      Never consumes the email — only the files are copied out; `filed` is
            //      deliberately not set. ----
            if (anchor is not null && stagedDocControlIds.Count > 0)
            {
                busyLabel = "Sending to Document Triage";
                await Commands.SendAsync(
                    new SendAttachmentsToDocumentControl(
                        anchor.Id, anchor.InternetMessageId,
                        stagedDocControlIds.ToList(), NullIfBlank(triageProjectId)),
                    CancellationToken.None);
                // One send per apply: clear the ticks (the server skips already-sent ids
                // regardless).
                stagedDocControlIds.Clear();
            }

            // ---- Section 2: to-dos (their command verifies every tag before saving) ----
            if (drafts.Count > 0)
            {
                busyLabel = "Creating to-dos";
                // No request link here: to-dos are their own concern, and linking the email to a
                // record — a request included — is the filing section's job.
                await Intake.CreateTodoItemsFromMessageAsync(new CreateTodoItemsFromMessage(
                    anchor.Id,
                    NullIfBlank(triageProjectId),
                    drafts,
                    LinkRequestId: null,
                    InternetMessageId: anchor.InternetMessageId,
                    Pathway: pathway is { } chosenForTodos ? PathwayLabel(chosenForTodos) : null,
                    Scope: scope));
                // One batch per apply: clear the rows so nothing can double-raise.
                createTodoRows = new List<TodoDraftRow> { new() };
                filed = true;
            }

            // ---- Record filing: every staged link applies, whatever picker is open ----
            if (anchor is not null && picks.Count > 0)
            {
                busyLabel = "Linking";
                foreach (var record in picks)
                {
                    // AllowCrossPathway: true — the pane choice IS the cross-filing decision
                    // (confirm retired 2026-08-28; true also keeps an older api from prompting).
                    await Intake.LinkMessageToRecordAsync(
                        anchor.Id, anchor.InternetMessageId, record.Type, record.RecordId,
                        pathway: CostCentrePathwayFor(record),
                        allowCrossPathway: true,
                        scope: scope);
                    filed = true;
                }
            }
            // ---- The thread's existing tags, answered Yes above: each resolved record links
            //      exactly like a picked one. Records the triager ALSO picked by hand are
            //      skipped — one link per record per apply. allowCrossPathway is true outright:
            //      these tags are already on the thread, so re-filing this reply under them is
            //      never a new cross-pathway decision. ----
            if (anchor is not null && inheritedRecords.Count > 0)
            {
                busyLabel = "Linking to the thread's tags";
                foreach (var record in inheritedRecords)
                {
                    if (picks.Any(pick => pick.Type == record.Type
                        && string.Equals(pick.RecordId, record.RecordId, StringComparison.Ordinal)))
                        continue;
                    await Intake.LinkMessageToRecordAsync(
                        anchor.Id, anchor.InternetMessageId, record.Type, record.RecordId,
                        pathway: CostCentrePathwayFor(record),
                        allowCrossPathway: true,
                        scope: scope);
                    filed = true;
                }
            }
            // A Relevant Event answered Yes: link the thread to the project's programme bucket — the
            // record id IS the project id (one bucket per project, SchedulingLinkProvider).
            // Scheduling is a Client-side record, so on a non-client thread this cross-files the
            // thread — allowed without a confirm, like the picks above.
            if (relevantEvent)
            {
                busyLabel = "Tagging relevant event";
                await Intake.LinkMessageToRecordAsync(
                    anchor.Id, anchor.InternetMessageId, RecordType.Scheduling, triageProjectId,
                    pathway: null,
                    allowCrossPathway: true,
                    scope: scope);
                filed = true;
            }
            if (createReady && stagedCreate is { } staged)
            {
                var created = await RaiseStagedRecordAsync(staged, anchor!, scope);
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
            // ---- System actions lined up in the Actions pane — run once the filing above has
            //      landed, each removed as it succeeds so a failed one can be retried without
            //      re-running its predecessors. A failure stops the apply with its reason. ----
            foreach (var stagedAction in stagedSystemActions.ToList())
            {
                busyLabel = $"System action: {SystemActionKinds.Label(stagedAction.Kind)}";
                await stagedAction.ExecuteAsync();
                stagedSystemActions.Remove(stagedAction);
            }

            if (discarding)
            {
                // "File it as nothing": tag the thread discarded — restorable from the Tagged tab.
                // Runs after the to-dos so "capture the follow-ups, then bin the email" works.
                busyLabel = "Discarding";
                await Intake.DiscardMessageAsync(anchor.Id, anchor.InternetMessageId);
                filed = true;
            }

            // ---- The Outbox: replies lined up against OLDER emails. Each anchor email is first
            //      tagged to the triage's record picks (one triage decision covers every email
            //      answered — decision 2026-08-12), then the reply sends; the server files the
            //      sent copy by the anchor's tags, the fresh ones included, because the links
            //      land before the send. MessageOnly spread: the reply answers THAT email; the
            //      selected email's thread decision doesn't reach into other conversations. Each
            //      entry is removed as it completes, so a failure stops the apply with the
            //      already-sent replies never re-sent. ----
            var outboxSent = 0;
            foreach (var lined in queuedReplies.ToList())
            {
                foreach (var record in picks)
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
                    : $"{outboxSent} lined-up {(outboxSent == 1 ? "email was" : "emails were")} sent from the projects mailbox{(picks.Count > 0 ? ", each email tagged to the picked records" : "")}.";

            // ---- Section 1: the reply (or forward) — last, so nothing above can be lost to a
            //      send failure. When a filing already dealt with the thread its record tag says
            //      more than Replied, so the stamp is skipped — and a FORWARD never stamps: it
            //      passes the email on rather than answering it, so the email stays queued
            //      unless a filing above dealt with it. ----
            if (replying)
            {
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

            // Applied in full: refresh the queue in place — the triager stays on the page they
            // were working — and clear the selection (the email has left it). The Triage tab
            // hands back to the queue list, ready for the next email.
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

    /// <summary>What one staged-create execution produced: the created-record chip for the pane,
    /// and — for a work order whose picked files failed to upload — the error that stops the
    /// caller (the order exists and the email is tagged; the files are re-added from the order's
    /// PO page).</summary>
    // ---- New email (fresh outbound thread from the projects mailbox) ----

    // Clears the compose form and hands its window back to whatever it showed before — pressed
    // as Cancel, and called after a successful send so the outcome banner is what remains.
    private void CloseNewEmail()
    {
        if (newEmailBusy) return;
        workspace.Close(PanelKind.Compose);
        newEmailError = null;
        newEmailTo = newEmailCc = newEmailBcc = newEmailSubject = newEmailBody = "";
        newEmailAttachments = Array.Empty<ComposeDraftAttachment>();
        newEmailFile = false;
        newEmailProjectId = "";
        newEmailRecordType = RecordType.Request;
        newEmailRecordId = "";
        newEmailRecords = Array.Empty<LinkableRecord>();
    }

    private bool NewEmailIsSendable =>
        ParseRecipients(newEmailTo).Count > 0
        && !string.IsNullOrWhiteSpace(newEmailSubject)
        && HtmlHasContent(newEmailBody)
        && (!newEmailFile || (!string.IsNullOrEmpty(newEmailProjectId) && !string.IsNullOrEmpty(newEmailRecordId)));

    private void OnNewEmailToInput(ChangeEventArgs e) => newEmailTo = e.Value?.ToString() ?? "";
    private void OnNewEmailCcInput(ChangeEventArgs e) => newEmailCc = e.Value?.ToString() ?? "";
    private void OnNewEmailBccInput(ChangeEventArgs e) => newEmailBcc = e.Value?.ToString() ?? "";
    private void OnNewEmailSubjectInput(ChangeEventArgs e) => newEmailSubject = e.Value?.ToString() ?? "";

    private void OnNewEmailBodyChanged(string html) => newEmailBody = html;
    private void OnNewEmailAttachmentsChanged(IReadOnlyList<ComposeDraftAttachment> attachments) =>
        newEmailAttachments = attachments;

    private void OnNewEmailFileToggled(ChangeEventArgs e)
    {
        newEmailFile = e.Value is true;
        if (!newEmailFile) { newEmailRecordId = ""; }
    }

    private async Task OnNewEmailProjectChanged(ChangeEventArgs e)
    {
        newEmailProjectId = e.Value?.ToString() ?? "";
        newEmailRecordId = "";
        await LoadNewEmailRecordsAsync();
    }

    private async Task OnNewEmailRecordTypeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var t)) newEmailRecordType = (RecordType)t;
        newEmailRecordId = "";
        await LoadNewEmailRecordsAsync();
    }

    private void OnNewEmailRecordChanged(ChangeEventArgs e) => newEmailRecordId = e.Value?.ToString() ?? "";

    private async Task LoadNewEmailRecordsAsync()
    {
        newEmailRecords = Array.Empty<LinkableRecord>();
        if (string.IsNullOrEmpty(newEmailProjectId)) return;
        newEmailRecordsLoading = true;
        try { newEmailRecords = await Intake.ListLinkableRecordsAsync(newEmailProjectId, newEmailRecordType); }
        catch { newEmailError = "Couldn't load the records for that project. Please try again."; }
        finally { newEmailRecordsLoading = false; }
    }

    private async Task DoSendNewEmail(bool saveAsDraftOnly)
    {
        if (newEmailBusy) return;
        var to = ParseRecipients(newEmailTo);
        if (to.Count == 0) { newEmailError = "Add a To recipient."; return; }
        if (string.IsNullOrWhiteSpace(newEmailSubject)) { newEmailError = "Write a subject."; return; }
        if (!HtmlHasContent(newEmailBody)) { newEmailError = "Write the email first."; return; }

        var command = new SendMailboxEmail(
            ReplyToMessageId: null,
            ReplyToInternetMessageId: null,
            To: to,
            Cc: ParseRecipients(newEmailCc),
            Bcc: ParseRecipients(newEmailBcc),
            Subject: newEmailSubject.Trim(),
            Body: newEmailBody,
            BodyIsHtml: true,
            Attachments: newEmailAttachments.Select(a => a.ToRef()).ToList(),
            SaveAsDraftOnly: saveAsDraftOnly,
            Pathway: null,
            MarkThreadHandled: false,
            LinkRecordType: newEmailFile && !string.IsNullOrEmpty(newEmailRecordId) ? newEmailRecordType : null,
            LinkRecordId: newEmailFile && !string.IsNullOrEmpty(newEmailRecordId) ? newEmailRecordId : null,
            ProjectId: newEmailFile && !string.IsNullOrEmpty(newEmailProjectId) ? newEmailProjectId : null);
        var uploadParts = UploadPartsOf(newEmailAttachments);

        newEmailError = null;
        newEmailBusy = true;
        try
        {
            composeOutcome = await Intake.SendComposedEmailAsync(command, uploadParts);
            newEmailBusy = false;
            CloseNewEmail();
        }
        catch (CommandFailedException ex)
        {
            newEmailError = ex.Message;
        }
        catch
        {
            newEmailError = "The send didn't complete. Please try again.";
        }
        finally { newEmailBusy = false; }
    }

    private void OnReplyBodyInput(ChangeEventArgs e) => replyBody = e.Value?.ToString() ?? "";

    private async Task DoRestore()
    {
        if (selected is null || busy) return;
        actionError = null;
        try
        {
            busyLabel = "Restoring";
            busy = true;
            await Intake.RestoreMessageAsync(selected.Id, selected.InternetMessageId);
            selected = null;
            detail = null;
            detailLoading = false;
            ReturnWorkspaceToQueue();
            await ReloadDiscardedInPlaceAsync();
        }
        catch
        {
            actionError = "Couldn't restore that email. Please try again.";
        }
        finally { busy = false; }
    }

}
