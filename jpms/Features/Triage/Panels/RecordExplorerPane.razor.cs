using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Features.Triage.Workspace;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Features.Triage.Panels;

public partial class RecordExplorerPane
{
    /// <summary>The pickable projects — supplied by the page so the explorer honours the same
    /// completed-projects preference as every other picker.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();

    /// <summary>Raised when a document inside the open record asks to be viewed — the workspace
    /// shows it in the Preview pane on the opposite window.</summary>
    [Parameter] public EventCallback<PreviewRequest> OnPreview { get; set; }

    /// <summary>Raised when a Reply is started on an email in the open record's correspondence —
    /// the page lines the reply up in the Outbox. No delegate, no Reply buttons.</summary>
    [Parameter] public EventCallback<MailboxMessage> OnReply { get; set; }

    private const int ResultCap = 50;

    private string projectId = "";
    private RecordType recordType = RecordType.Request;
    private string search = "";
    // The explorer defaults to each type's live records; ticking the box lists the closed,
    // rejected and completed ones too — history is exactly what an explorer is for.
    private bool shouldIncludeInactive;
    private bool loading;
    // Null until a search context exists — distinct from an empty answer, which is a real "none".
    private IReadOnlyList<LinkableRecord>? records;
    private LinkableRecord? openRecord;

    private IReadOnlyList<LinkableRecord> FilteredRecords
    {
        get
        {
            if (records is null) return Array.Empty<LinkableRecord>();
            var pool = shouldIncludeInactive ? records : records.Where(record => record.IsActive);
            var needle = search.Trim();
            var matches = needle.Length == 0
                ? pool
                : pool.Where(record => Matches(record, needle));
            return matches.Take(ResultCap).ToList();
        }
    }

    private int HiddenInactiveCount => records?.Count(record => !record.IsActive) ?? 0;

    private static bool Matches(LinkableRecord record, string needle) =>
        record.Reference.Contains(needle, StringComparison.OrdinalIgnoreCase)
        || record.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
        || (record.Summary?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);

    private async Task OnProjectChanged(ChangeEventArgs e)
    {
        projectId = e.Value?.ToString() ?? "";
        await LoadAsync();
    }

    private async Task OnTypeChanged(ChangeEventArgs e)
    {
        if (Enum.TryParse<RecordType>(e.Value?.ToString(), out var type)) recordType = type;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        openRecord = null;
        loading = true;
        try
        {
            records = string.IsNullOrWhiteSpace(projectId)
                ? recordType == RecordType.Request ? await LoadRfisAcrossProjectsAsync() : null
                : await Intake.ListLinkableRecordsAsync(projectId, recordType);
        }
        catch
        {
            records = Array.Empty<LinkableRecord>();
        }
        finally
        {
            loading = false;
        }
    }

    // The cross-project RFI register, reshaped into the explorer's record-agnostic rows.
    private async Task<IReadOnlyList<LinkableRecord>> LoadRfisAcrossProjectsAsync()
    {
        var rfis = await Queries.AskAsync(new ListRfisAcrossProjects(), CancellationToken.None);
        return rfis
            .Select(rfi => new LinkableRecord(
                RecordType.Request, rfi.RequestId, rfi.ProjectId, rfi.Reference, rfi.Reference,
                rfi.Title, StatusLabel: rfi.Status.DisplayName(),
                Summary: rfi.Description,
                IsActive: rfi.Status != RequestStatus.Closed))
            .ToList();
    }
}
