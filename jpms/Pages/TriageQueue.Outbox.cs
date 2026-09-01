using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
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

    // The Discarded tab's one action: un-discard the open email, back into the live queue.
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

}
