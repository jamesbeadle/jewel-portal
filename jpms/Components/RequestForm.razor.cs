using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Components;

public partial class RequestForm : IDisposable
{
    /// <summary>A valid submission: the command as it would be sent, plus the attachments chosen
    /// (empty when the tray is hidden) — the HOST decides whether to send now or stage it.</summary>
    public sealed record Draft(
        RaiseRequest Command,
        IReadOnlyList<string> DrawingRevisionIds,
        IReadOnlyList<IBrowserFile> Files);

    [Parameter] public string ProjectId { get; set; } = "";

    /// <summary>Raised by <see cref="TrySubmitAsync"/> when the form validates.</summary>
    [Parameter] public EventCallback<Draft> OnSubmit { get; set; }

    /// <summary>Hide the attachment tray for hosts whose sink can't apply attachments yet
    /// (a staged draft has no request id until it fires).</summary>
    [Parameter] public bool ShowAttachments { get; set; } = true;

    /// <summary>The host's in-flight flag — disables the attachment controls while it works.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>Lock the form to raising an official RFI: no "Raise as" toggle, RFI defaults from
    /// the start. The RFIs page's manual "Raise RFI" uses this — General requests are sunset
    /// (2026-08-14) and nothing raises a new one.</summary>
    [Parameter] public bool LockToRfi { get; set; }

    private static readonly RequestStatus[] StatusOptions =
    {
        RequestStatus.NeedsAction, RequestStatus.Open,
        RequestStatus.NeedsVariation, RequestStatus.Closed
    };

    private RequestType kind = RequestType.General;
    private string reference = "";
    private string suggestedReference = "";
    private string title = "";
    private string description = "";
    private string drawingRef = "";
    private string responseDue = RequestDefaults.ResponseDue();
    private string valueText = "";
    private bool backfill;
    private string raisedAt = "";
    private string respondedAt = "";
    private string responseText = "";
    private RequestStatus status = RequestType.General.DefaultStatusOnRaise();
    private bool statusChosenByUser;
    private string? error;

    private sealed record RevisionOption(string RevisionId, string Code, string Label, string Title);
    private readonly List<RevisionOption> selectedRevisions = new();
    private readonly List<IBrowserFile> pendingFiles = new();
    private bool drawingPickerOpen;

    protected override void OnInitialized()
    {
        // The drawing store fills in the background and publishes OnChange when it lands; the
        // warm-up on mount means "Attach drawings" has something in it the first time it's pressed.
        Drawings.OnChange += StateHasChanged;
        if (ShowAttachments && !string.IsNullOrEmpty(ProjectId)) Drawings.Refresh(ProjectId);
        // A locked form starts on RFI (with the next-number suggestion), not on the retired General.
        if (LockToRfi) SetKind(RequestType.Rfi);
    }

    public void Dispose() => Drawings.OnChange -= StateHasChanged;

    /// <summary>Back to a blank form — the dialog calls this on open, the pane after staging.</summary>
    public void Reset()
    {
        title = description = drawingRef = valueText = "";
        responseDue = RequestDefaults.ResponseDue();
        backfill = false;
        raisedAt = respondedAt = responseText = "";
        kind = RequestType.General;
        status = kind.DefaultStatusOnRaise();
        statusChosenByUser = false;
        reference = suggestedReference = "";
        error = null;
        selectedRevisions.Clear();
        pendingFiles.Clear();
        drawingPickerOpen = false;
        if (LockToRfi) SetKind(RequestType.Rfi); // back to RFI, with a fresh next-number suggestion
        // Warm the drawing register from here, never from render, so "Attach drawings" has
        // something in it the first time it is pressed.
        if (ShowAttachments && !string.IsNullOrEmpty(ProjectId)) Drawings.Refresh(ProjectId);
        StateHasChanged();
    }

    /// <summary>Pre-select the kind (the System Actions "Raise RFI" entry opens straight on RFI).</summary>
    public void PresetKind(RequestType preset) => SetKind(preset);

    /// <summary>Validate and, when valid, hand the draft to the host through OnSubmit.
    /// False (with the error shown inline) when something is missing.</summary>
    public async Task<bool> TrySubmitAsync()
    {
        error = null;

        if (string.IsNullOrWhiteSpace(ProjectId)) { error = "Pick the project first."; return false; }
        if (string.IsNullOrWhiteSpace(title)) { error = "A subject is required."; return false; }

        var email = Auth.CurrentUser?.Email;
        if (string.IsNullOrWhiteSpace(email)) { error = "You must be signed in to raise a request."; return false; }

        decimal? value = null;
        if (!string.IsNullOrWhiteSpace(valueText))
        {
            if (!decimal.TryParse(valueText, out var parsed)) { error = "Value must be a number."; return false; }
            value = parsed;
        }

        // Blank reference: the server mints the number (REQ-#### for General, the project's next
        // RFI-nnn for an RFI). An untouched suggestion is sent blank so the server stays
        // authoritative; only a genuinely edited reference (legacy back-fill) is sent as typed.
        var referenceToSend = kind == RequestType.Rfi
            && !string.IsNullOrWhiteSpace(reference)
            && !string.Equals(reference.Trim(), suggestedReference, StringComparison.OrdinalIgnoreCase)
                ? reference.Trim()
                : "";

        var command = new RaiseRequest(
            ProjectId,
            kind,
            referenceToSend,
            title.Trim(),
            description?.Trim() ?? "",
            value,
            email,
            DrawingRef: NullIfBlank(drawingRef),
            ResponseDue: ParseDate(responseDue),
            RaisedAt: backfill ? ParseDate(raisedAt) : null,
            RespondedAt: backfill ? ParseDate(respondedAt) : null,
            ResponseText: backfill ? NullIfBlank(responseText) : null,
            RespondedByEmail: backfill && !string.IsNullOrWhiteSpace(responseText) ? email : null,
            Status: backfill ? status : null);

        await OnSubmit.InvokeAsync(new Draft(
            command,
            selectedRevisions.Select(revision => revision.RevisionId).ToList(),
            pendingFiles.ToList()));
        return true;
    }

    /// <summary>A host-side send failure, shown inline where the fields are.</summary>
    public void ShowError(string message)
    {
        error = message;
        StateHasChanged();
    }

    private List<RevisionOption> PickableRevisions =>
        Drawings.DrawingsFor(ProjectId)
            .SelectMany(drawing => Drawings.RevisionsFor(drawing.DrawingId)
                .OrderByDescending(revision => revision.ReceivedAt)
                .Select(revision => new RevisionOption(
                    revision.DrawingRevisionId, DrawingNaming.Code(drawing), revision.RevisionLabel, DrawingNaming.Name(drawing))))
            .ToList();

    private void OpenDrawingPicker()
    {
        Drawings.Refresh(ProjectId); // fetch on open, never from render
        drawingPickerOpen = true;
    }

    private void ToggleRevision(RevisionOption revision, bool selected)
    {
        selectedRevisions.RemoveAll(r => r.RevisionId == revision.RevisionId);
        if (selected) selectedRevisions.Add(revision);
    }

    private void OnFilesSelected(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles(20))
            if (!pendingFiles.Any(existing => existing.Name == file.Name && existing.Size == file.Size))
                pendingFiles.Add(file);
    }

    private void SetKind(RequestType selected)
    {
        if (kind == selected) return;
        kind = selected;
        // An official RFI is with the architect the moment it is raised; a General container is
        // ours. Track that as the kind toggles, unless the user has already set the status.
        if (!statusChosenByUser) status = kind.DefaultStatusOnRaise();
        if (kind == RequestType.Rfi)
        {
            // Suggest the project's next RFI number from the local register (the server re-derives
            // it on save, so this is a preview the user can overtype for legacy back-fills).
            suggestedReference = RequestReference.SuggestNext(
                RequestType.Rfi,
                RequestRegister.ForProject(ProjectId).Select(r => r.Reference));
            reference = suggestedReference;
        }
        else
        {
            reference = suggestedReference = "";
        }
    }

    private string KindButtonClass(RequestType option) =>
        kind == option
            ? "px-3 py-1.5 rounded-lg text-sm font-medium bg-accent/15 border border-accent text-content"
            : "px-3 py-1.5 rounded-lg text-sm font-medium bg-surface-raised border border-line text-content-muted hover:text-content";

    private void OnStatusChanged(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out var raw)) return;
        status = (RequestStatus)raw;
        statusChosenByUser = true;
    }

    // Logging a HISTORICAL request clears the pre-filled response due: a week from today is a
    // sensible default for a request being raised now, and nonsense for one raised months ago.
    private void OnBackfillChanged(ChangeEventArgs e)
    {
        backfill = e.Value is true;
        if (backfill && responseDue == RequestDefaults.ResponseDue()) responseDue = "";
        else if (!backfill && string.IsNullOrWhiteSpace(responseDue)) responseDue = RequestDefaults.ResponseDue();
    }

    private void OnDescriptionInput(ChangeEventArgs e) => description = e.Value?.ToString() ?? "";

    private void OnResponseTextInput(ChangeEventArgs e) => responseText = e.Value?.ToString() ?? "";

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}
