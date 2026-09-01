using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Features.Drawings;

namespace Jewel.JPMS.Features.Triage;

public partial class AttachmentPicker
{
    /// <summary>The attachments currently on the composed email. Owned by the parent; this
    /// component only proposes additions/removals through <see cref="AttachmentsChanged"/>.</summary>
    [Parameter] public IReadOnlyList<ComposeDraftAttachment> Attachments { get; set; } = Array.Empty<ComposeDraftAttachment>();
    [Parameter] public EventCallback<IReadOnlyList<ComposeDraftAttachment>> AttachmentsChanged { get; set; }

    /// <summary>Projects offered by the drawings/photos panels' project select.</summary>
    [Parameter] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();

    /// <summary>Pre-seeds the panels' project select (e.g. the reply form's chosen project).</summary>
    [Parameter] public string? ProjectId { get; set; }

    /// <summary>The opened email's own attachments, offered for forwarding; null hides the tab.</summary>
    [Parameter] public IReadOnlyList<IntakeAttachment>? OriginalAttachments { get; set; }
    [Parameter] public string? OriginalMessageId { get; set; }

    private const long MaxFileBytes = 25_000_000;

    private enum Panel { None, Drawings, Photos, Records, Original }
    private Panel openPanel = Panel.None;
    private string panelProjectId = "";
    private string? expandedDrawingId;
    private string? oversizeNote;

    // Drawings panel state: the register rendered as its folder tree (reusing DrawingFolderTree,
    // the same nesting the register table draws), a search box that flattens matches, and a
    // multi-select basket. Ticking a drawing means "its best file" (approved, else latest upload),
    // resolved when Attach is pressed; expanding a drawing lets a specific revision be ticked.
    private string drawingSearch = "";
    private readonly HashSet<string> collapsedFolderIds = new();
    private readonly HashSet<string> selectedDrawingIds = new();
    private readonly Dictionary<string, DrawingRevision> selectedRevisionsById = new();
    private bool attachingSelected;
    private string? attachNote;
    private DrawingFolderTree drawingTree = new(Array.Empty<DrawingFolder>());
    private const int DrawingResultCap = 100;
    private const string UngroupedFolderKey = "";
    private const double DrawingIndentRem = 1.25;

    private sealed record DrawingGroup(string Key, DrawingFolder? Folder, int Depth, IReadOnlyList<Drawing> Drawings);

    // System-documents panel state. Records are fetched once per project for the composer's life
    // (a picker list, not a live view); absence from the dictionary is the honest "not fetched
    // yet", so the panel shows Loading rather than an empty answer it hasn't got.
    private readonly Dictionary<string, IReadOnlyList<LinkableRecord>> recordsByProject = new();
    private string? recordsFailedFor;
    private string recordSearch = "";
    private bool recordsIncludeInactive;
    private const int RecordResultCap = 50;

    private IReadOnlyList<LinkableRecord> FilteredRecords
    {
        get
        {
            if (!recordsByProject.TryGetValue(panelProjectId, out var pool)) return Array.Empty<LinkableRecord>();
            var live = recordsIncludeInactive ? pool : pool.Where(r => r.IsActive);
            var needle = recordSearch.Trim();
            var matches = needle.Length == 0
                ? live
                : live.Where(r => r.Reference.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || r.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || (r.Summary?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false));
            return matches.Take(RecordResultCap).ToList();
        }
    }

    protected override void OnInitialized()
    {
        Drawings.OnChange += StoreChanged;
        Progress.OnChange += StoreChanged;
    }

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(panelProjectId) && !string.IsNullOrEmpty(ProjectId))
            panelProjectId = ProjectId!;
    }

    private void StoreChanged() => InvokeAsync(() =>
    {
        // Folders may have just landed (or changed elsewhere) — rebuild the nesting they define.
        RebuildDrawingTree();
        StateHasChanged();
    });

    private async Task TogglePanel(Panel panel)
    {
        openPanel = openPanel == panel ? Panel.None : panel;
        oversizeNote = null;
        if (panel is Panel.Drawings) ResetDrawingPanelState(); // opening starts clean; closing drops a stale basket
        if (openPanel is Panel.Records) recordsFailedFor = null; // reopening retries a failed load
        if (string.IsNullOrEmpty(panelProjectId)) return;
        if (openPanel is Panel.Drawings) Drawings.Refresh(panelProjectId);
        if (openPanel is Panel.Photos) Progress.Refresh(panelProjectId);
        if (openPanel is Panel.Records) await LoadRecordsAsync(panelProjectId);
    }

    private async Task OnPanelProjectChanged(ChangeEventArgs e)
    {
        panelProjectId = e.Value?.ToString() ?? "";
        ResetDrawingPanelState(); // a basket of one project's drawings must not survive into another
        if (string.IsNullOrEmpty(panelProjectId)) return;
        if (openPanel == Panel.Drawings) Drawings.Refresh(panelProjectId);
        if (openPanel == Panel.Photos) Progress.Refresh(panelProjectId);
        if (openPanel == Panel.Records) await LoadRecordsAsync(panelProjectId);
    }

    private async Task LoadRecordsAsync(string projectId)
    {
        if (recordsByProject.ContainsKey(projectId)) return;
        try
        {
            // Both record families that carry an official PDF: requests (RFI, NOD, EOT…) and
            // variation orders. The variation provider lists every stage of a project's
            // variations — a still-quoting order is offered under its VOQ identity, an approved
            // one under its V-ref — so a VO can be attached and sent at any point in its life.
            var requests = await Intake.ListLinkableRecordsAsync(projectId, RecordType.Request);
            var variations = await Intake.ListLinkableRecordsAsync(projectId, RecordType.Variation);
            // Tender enquiries carry the PQQ response — the third official document.
            var tenderEnquiries = await Intake.ListLinkableRecordsAsync(projectId, RecordType.TenderEnquiry);
            recordsByProject[projectId] = requests.Concat(variations).Concat(tenderEnquiries).ToList();
        }
        catch
        {
            // The query client has reported the failure; the panel says so and reopening retries.
            recordsFailedFor = projectId;
        }
    }

    private void ToggleDrawing(string drawingId)
    {
        expandedDrawingId = expandedDrawingId == drawingId ? null : drawingId;
        // Expanding is the ask: the "Loading revisions…" row never reads RevisionsFor (only the
        // loaded branch does), so without this kick the fetch never started and the row said
        // "Loading revisions…" forever (reported 2026-08-28). The read fires the one-time
        // background load; the store's OnChange re-renders when it lands.
        if (expandedDrawingId is not null) _ = Drawings.RevisionsFor(expandedDrawingId);
    }

}
