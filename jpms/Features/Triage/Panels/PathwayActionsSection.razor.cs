

namespace Jewel.JPMS.Features.Triage.Panels;

public partial class PathwayActionsSection
{
    /// <summary>The full project list — the staged store calls read it only to name the project
    /// in their staged summaries.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();

    /// <summary>The email's project, picked once in the triage bar; blank shows the note above
    /// instead of a project-bound form (to-dos and directory contacts work without one).</summary>
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    /// <summary>Page-owned staged actions (cleared per selection, like all staging); the pane
    /// adds/removes and notifies.</summary>
    [Parameter, EditorRequired] public List<StagedSystemAction> StagedActions { get; set; } = new();
    [Parameter] public EventCallback OnStagingChanged { get; set; }

    /// <summary>The page's staged tag picks — the same list the pathway panes mutate. An
    /// action on an existing record stages that record's tag here the moment it stages, so the
    /// email is filed against what it actioned when Apply links the picks (links land before
    /// actions run).</summary>
    [Parameter, EditorRequired] public List<LinkableRecord> PickedRecords { get; set; } = new();

    /// <summary>The staged new record (null = none) — the email-linked creates' one draft. Owned
    /// by the page; edited in <see cref="StagedRecordActionEditor"/>.</summary>
    [Parameter] public StagedRecordCreate? StagedCreate { get; set; }
    [Parameter] public EventCallback<StagedRecordCreate?> StagedCreateChanged { get; set; }

    /// <summary>Records already raised from this email — by Create now, or by an apply whose
    /// create landed before a later step stopped it — shown as done chips with their minted
    /// references. Page-owned, parked/restored per email like the staging.</summary>
    [Parameter] public List<CreatedNowRecord> CreatedRecords { get; set; } = new();

    /// <summary>The staged chip's "Create now": the page raises the staged record immediately
    /// (email tagged in the same act) instead of waiting for Apply.</summary>
    [Parameter] public EventCallback OnCreateNow { get; set; }

    /// <summary>The page's busy flag — disables Create now while any apply/create is running.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Done pressed under a record form, or Stage action pressed on a store call — the
    /// page hands this window back to the email.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    // The actions that draft the page's staged record — the ones the footer finishes.
    private static bool IsRecordCreate(SystemActionKind actionKind) =>
        actionKind is SystemActionKind.RaiseRfi
            or SystemActionKind.RaiseWorkOrder
            or SystemActionKind.CreateBidPackageInvite
            or SystemActionKind.RaiseDefect
            or SystemActionKind.AddInventoryItem
            or SystemActionKind.RaiseSiteInstruction
            or SystemActionKind.RaiseCalendarEvent
            or SystemActionKind.RaiseBuildingControlInspection;

    /// <summary>Page-owned to-do draft rows — "Create To-do Items". One to-do per assignee per
    /// row on Apply, the email tagged to every one.</summary>
    [Parameter] public List<TodoDraftRow> TodoRows { get; set; } = new();
    [Parameter] public IReadOnlyList<SearchSelect.Option> TodoAssigneeOptions { get; set; } = Array.Empty<SearchSelect.Option>();
    [Parameter] public string? TodoProjectNote { get; set; }

    /// <summary>The open email's attachments — the work-order create offers them as record-keeping
    /// tick-boxes.</summary>
    [Parameter] public IReadOnlyList<IntakeAttachment> EmailAttachments { get; set; } = Array.Empty<IntakeAttachment>();

    /// <summary>The selected email's sender — pre-fills the work-order subcontractor and the
    /// defect assignee. Suggestions only.</summary>
    [Parameter] public string SenderEmail { get; set; } = "";

    /// <summary>The dropdown's groups — the pane's own side of the action set. Null = the full
    /// guide (every group), which no pane uses but keeps the component honest standalone.</summary>
    [Parameter] public IReadOnlyList<(string Title, IReadOnlyList<SystemActionKind> Kinds)>? Groups { get; set; }

    [Inject] private SessionService Session { get; set; } = default!;

    // The pane's groups, minus the administrator-only kinds (Mark as KPI) for every other active
    // role — an administrator viewing as a PM is not offered them either: the register they
    // feed is administrators-only, and the API refuses the write for anyone else anyway.
    private IReadOnlyList<(string Title, IReadOnlyList<SystemActionKind> Kinds)> EffectiveGroups
    {
        get
        {
            var groups = Groups ?? SystemActionGuide.Groups;
            if (Session.ActiveRole == Role.Admin) return groups;
            return groups
                .Select(group => (group.Title, (IReadOnlyList<SystemActionKind>)group.Kinds
                    .Where(kind => !SystemActionGuide.AdministratorOnly.Contains(kind)).ToList()))
                .Where(group => group.Item2.Count > 0)
                .ToList();
        }
    }

    private SystemActionKind? chosenKind;

    // The selected action: the triager's choice while it belongs to this pane's groups, else the
    // pane's first offer (the groups differ per pathway, so a stale choice must not linger).
    private SystemActionKind kind
    {
        get
        {
            var allowed = EffectiveGroups.SelectMany(group => group.Kinds).ToList();
            return chosenKind is { } chosen && allowed.Contains(chosen)
                ? chosen
                : allowed.FirstOrDefault(SystemActionKind.RaiseRfi);
        }
        set => chosenKind = value;
    }

    // To-dos are company-wide until a project is set and a directory contact has no project at
    // all — everything else raises on the email's project.
    private static bool NeedsProject(SystemActionKind actionKind) =>
        actionKind is not (SystemActionKind.AddDirectoryContact or SystemActionKind.CreateTodos
            or SystemActionKind.MarkAsKpi);

    /// <summary>The open email's mailbox ids — "Mark as KPI" files the email by them (no tag, so
    /// the page's link path is not involved).</summary>
    [Parameter] public string MessageId { get; set; } = "";
    [Parameter] public string? InternetMessageId { get; set; }

    /// <summary>The open email's To/Cc addresses — "Mark as KPI" pre-matches the portal user
    /// from the sender first, then these.</summary>
    [Parameter] public IReadOnlyList<string> RecipientEmails { get; set; } = Array.Empty<string>();

    /// <summary>The open email's subject — the tender enquiry editor titles the enquiry from it.</summary>
    [Parameter] public string EmailSubject { get; set; } = "";


    private List<TodoDraftRow> TitledTodoRows => TodoRows.Where(r => !string.IsNullOrWhiteSpace(r.Title)).ToList();

    private void OnKindChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<SystemActionKind>(e.Value?.ToString(), out var parsed)) kind = parsed;
    }

    /// <summary>The pane this section serves ("Client", "Supplier", …) — stamped onto every
    /// staged action so badges count by the pane that staged it.</summary>
    [Parameter] public string Pathway { get; set; } = "";

    private async Task StagedAsync(StagedSystemAction action)
    {
        action = action with { Pathway = string.IsNullOrWhiteSpace(Pathway) ? null : Pathway };
        // An action on an existing record tags the email to it: the target's pick is staged the
        // moment the action stages, exactly as if it had been ticked in System Tags by hand. A
        // record the triager already picked stays theirs (TargetAutoTagged false), so removing
        // the action later never takes away a tag it didn't add.
        if (action.Target is { } target && !PickedRecords.Any(r => r.RecordId == target.RecordId))
        {
            PickedRecords.Add(target);
            action = action with { TargetAutoTagged = true };
        }
        StagedActions.Add(action);
        await OnStagingChanged.InvokeAsync();
        // Stage action pressed = done here (James, 2026-09-03: the form staying open after the
        // click read as "nothing happened", so it got pressed again). Hand the window back to
        // the email the same way Done does — the triage bar's "This will run N system action(s)"
        // line and the tab badge show the action waiting for Apply; Remove is on the Actions tab.
        await OnClose.InvokeAsync();
    }

    private async Task RemoveAsync(StagedSystemAction action)
    {
        StagedActions.Remove(action);
        // Take back the tag this action staged — unless another staged action still targets the
        // same record (its tag is still owed), or the pick was the triager's own to begin with.
        if (action is { TargetAutoTagged: true, Target: { } target }
            && !StagedActions.Any(a => a.Target?.RecordId == target.RecordId)
            && PickedRecords.FirstOrDefault(r => r.RecordId == target.RecordId) is { } picked)
        {
            PickedRecords.Remove(picked);
        }
        await OnStagingChanged.InvokeAsync();
    }

    /// <summary>Whether the action's target record is (still) among the staged tag picks — read
    /// live rather than snapshotted, so the row's "tagged to it" note stays honest if the pick
    /// is unticked in System Tags afterwards.</summary>
    private bool TargetIsTagged(StagedSystemAction action) =>
        action.Target is { } target && PickedRecords.Any(r => r.RecordId == target.RecordId);

    private async Task ClearStagedCreateAsync()
    {
        await StagedCreateChanged.InvokeAsync(null);
        await OnStagingChanged.InvokeAsync();
    }

    private async Task RemoveTodoRowAsync(TodoDraftRow row)
    {
        TodoRows.Remove(row);
        if (TodoRows.Count == 0) TodoRows.Add(new TodoDraftRow());
        await OnStagingChanged.InvokeAsync();
    }
}
