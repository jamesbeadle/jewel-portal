using Jewel.JPMS.Features.Triage.Workspace;

namespace Jewel.JPMS.Features.Triage.Panels;

public partial class PathwayPane
{
    private enum PaneTab { Tagging, Actions }

    /// <summary>Which pathway this pane is — its record types, family and action groups.</summary>
    [Parameter, EditorRequired] public PathwayPaneConfig Config { get; set; } = PathwayPaneConfig.Client;

    /// <summary>The email's project, picked once in the triage bar.</summary>
    [Parameter] public string ProjectId { get; set; } = "";

    /// <summary>The project's name — the category registers' "This project" scope.</summary>
    [Parameter] public string ProjectName { get; set; } = "";

    /// <summary>The open queue email's subject — blank when none is open, which puts the pane in
    /// browse mode: registers readable, tagging and actions waiting for an email.</summary>
    [Parameter] public string EmailSubject { get; set; } = "";

    /// <summary>The open email's id — keys the Actions section so a fresh selection starts
    /// fresh scratch forms (the staged work itself is page-owned and resets per selection).
    /// The registers deliberately are NOT keyed on it: they are standing browsers.</summary>
    [Parameter] public string EmailKey { get; set; } = "";

    /// <summary>The thread's filed pathway label, when it is already filed — drives the
    /// cross-filing hint. Null = not yet filed.</summary>
    [Parameter] public string? LockedPathway { get; set; }

    /// <summary>Page-owned staged record links — ONE list across all four panes; the pane
    /// mutates and notifies.</summary>
    [Parameter, EditorRequired] public List<LinkableRecord> Picked { get; set; } = new();
    [Parameter] public EventCallback OnStagingChanged { get; set; }

    /// <summary>Raised (with the pathway label) whenever the triager stages from this pane — the
    /// page records it as the email's pathway decision, as the old tab switch did.</summary>
    [Parameter] public EventCallback<string> OnEngaged { get; set; }

    /// <summary>Done pressed — the page lands this window back on the open email.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    // Register callbacks — the page lines replies/forwards up in the Outbox and opens previews.
    [Parameter] public EventCallback<PreviewRequest> OnPreview { get; set; }
    [Parameter] public EventCallback<MailboxMessage> OnReply { get; set; }
    [Parameter] public EventCallback<MailboxMessage> OnForward { get; set; }

    // Actions passthrough — the same page-owned state the retired System Actions pane took.
    [Parameter] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();
    [Parameter] public List<StagedSystemAction> StagedActions { get; set; } = new();
    [Parameter] public StagedRecordCreate? StagedCreate { get; set; }
    [Parameter] public EventCallback<StagedRecordCreate?> StagedCreateChanged { get; set; }
    [Parameter] public List<CreatedNowRecord> CreatedRecords { get; set; } = new();
    [Parameter] public EventCallback OnCreateNow { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public List<TodoDraftRow> TodoRows { get; set; } = new();
    [Parameter] public IReadOnlyList<SearchSelect.Option> TodoAssigneeOptions { get; set; } = Array.Empty<SearchSelect.Option>();
    [Parameter] public string? TodoProjectNote { get; set; }
    [Parameter] public IReadOnlyList<IntakeAttachment> EmailAttachments { get; set; } = Array.Empty<IntakeAttachment>();
    [Parameter] public string SenderEmail { get; set; } = "";
    [Parameter] public IReadOnlyList<TodoAssignablePerson> QsRecipients { get; set; } = Array.Empty<TodoAssignablePerson>();
    [Parameter] public EventCallback OnForwardToQs { get; set; }

    private bool HasOpenEmail => !string.IsNullOrWhiteSpace(EmailSubject);
    private bool HasActionsTab => Config.ActionGroups.Count > 0;

    private PaneTab activeTab = PaneTab.Tagging;

    private readonly HashSet<string> openSections = new();
    private string autoExpandedFor = "";

    private bool IsOpenSection(string key) => openSections.Contains(key);
    private void Toggle(string key)
    {
        if (!openSections.Add(key)) openSections.Remove(key);
    }

    // The first record section opens itself once per email (once the project is known), so
    // landing on the pane leads with the most likely tagging target and its candidate matches —
    // not a wall of closed drawers. The triager's own opens/closes are never overridden.
    protected override void OnParametersSet()
    {
        if (!HasOpenEmail || string.IsNullOrWhiteSpace(ProjectId) || Config.LinkTypes.Count == 0) return;
        var key = $"{EmailKey}|{ProjectId}";
        if (autoExpandedFor == key) return;
        autoExpandedFor = key;
        openSections.Add($"type:{Config.LinkTypes[0]}");
    }

    private int PickedCountFor(RecordType type) => Picked.Count(record => record.Type == type);

    /// <summary>The staged picks this pane owns — its record types plus its family records.</summary>
    private int TaggedCountHere =>
        Picked.Count(record => Config.LinkTypes.Contains(record.Type)
            || (Config.Family is { } family && family.All.Any(familyRecord => familyRecord.RecordId == record.RecordId)));

    // Counts by the pane that STAGED each action (kinds can be offered on more than one pane);
    // actions staged before the stamp existed fall back to the kind test.
    private int StagedActionCountHere =>
        StagedActions.Count(action => action.Pathway is { } stagedFrom
            ? stagedFrom == Config.Pathway
            : Config.AllActionKinds.Contains(action.Kind));

    private bool IsPicked(LinkableRecord record) =>
        Picked.Any(picked => picked.RecordId == record.RecordId);

    private async Task TogglePickAsync(LinkableRecord record)
    {
        var existing = Picked.FirstOrDefault(picked => picked.RecordId == record.RecordId);
        if (existing is null) Picked.Add(record);
        else Picked.Remove(existing);
        await StagingChangedAsync();
    }

    private async Task UnpickAsync(LinkableRecord record)
    {
        var existing = Picked.FirstOrDefault(picked => picked.RecordId == record.RecordId);
        if (existing is not null) Picked.Remove(existing);
        await StagingChangedAsync();
    }

    // Every staging change from this pane also reports the pathway decision — staging FROM a side
    // IS choosing that side, as switching the old System Tags tab was.
    private async Task StagingChangedAsync()
    {
        if (HasOpenEmail) await OnEngaged.InvokeAsync(Config.Pathway);
        await OnStagingChanged.InvokeAsync();
    }

    private async Task CloseAsync()
    {
        await OnStagingChanged.InvokeAsync();
        await OnClose.InvokeAsync();
    }

    // The staged chip reads the everyday name: the title for a valuation snapshot (its VRS-… stem
    // is a minted mail tag, decision 2026-08-20), the reference for everything else.
    private static string ChipLabel(LinkableRecord record) =>
        record.Type == RecordType.ValuationReportSnapshot ? record.Title : record.Reference;

    private string TabClass(PaneTab tab) =>
        "px-4 py-2 text-sm border-b-2 -mb-px transition inline-flex items-center gap-1.5 "
        + (activeTab == tab
            ? "border-accent text-content font-semibold"
            : "border-transparent text-content-muted hover:text-content");


}
