using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Features.Hs.Audits;

namespace Jewel.JPMS.Pages;

public partial class ProjectHsAudit
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string HsAuditId { get; set; } = "";

    private bool dataFailed;
    private bool busy;
    private string? actionError;
    private bool issueOpen;
    private bool closeOpen;
    private string managerName = "";

    private HsAuditType formType = HsAuditType.Routine;
    private string formInspectionDate = "";
    private string formSafetyOfficer = "";
    private string formSiteManager = "";
    private string formOperatives = "";
    private string formSummary = "";
    private string formFurtherComments = "";

    // The rows as the officer is editing them, keyed by item, rebuilt whenever a fresh view lands.
    private Dictionary<string, HsAuditItemDraft> drafts = new();
    private HsAuditView? adoptedView;

    private HsAuditView? View => Audits.View(HsAuditId);
    private bool IsEditable => View?.Audit.Status != HsAuditStatus.Closed;
    private decimal? LiveScore => HsAuditItemDraft.LiveScore(drafts.Values);
    private bool HasUnsavedChanges => drafts.Values.Any(draft => draft.IsDirty);

    private int FindingCount => drafts.Values.Count(draft =>
        draft.HsRecordId is null
        && draft.Comment != HsAuditComment.NotApplicable
        && (!string.IsNullOrWhiteSpace(draft.OwnerName) || draft.Rate is { } rate && rate < HsAuditRate.UpToDate));

    private IReadOnlyList<HsAuditItemDraft> DraftsFor(int section) =>
        drafts.Values.Where(draft => draft.Section == section).ToList();

    protected override async Task OnInitializedAsync()
    {
        Audits.OnChanged += OnAuditsChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try { await Audits.RefreshViewAsync(HsAuditId, CancellationToken.None); }
        catch { dataFailed = true; }
        AdoptView();
    }

    private void OnAuditsChanged()
    {
        AdoptView();
        StateHasChanged();
    }

    // A fresh view (a load, a save's answer, an issue) replaces the drafts and the header form;
    // the list refreshing behind the page is not a fresh view, so rows still being edited survive it.
    private void AdoptView()
    {
        if (View is not { } view || ReferenceEquals(view, adoptedView)) return;
        adoptedView = view;
        drafts = view.Items.ToDictionary(item => item.HsAuditItemId, item => new HsAuditItemDraft(item));
        AdoptHeader(view.Audit);
    }

    private void AdoptHeader(HsAudit audit)
    {
        formType = audit.Type;
        formInspectionDate = audit.InspectionDate.ToString("yyyy-MM-dd");
        formSafetyOfficer = audit.SafetyOfficerName;
        formSiteManager = audit.SiteManagerName;
        formOperatives = audit.SiteOperativeCount?.ToString() ?? "";
        formSummary = audit.SummaryOfWorkActivities;
        formFurtherComments = audit.FurtherComments;
        managerName = string.IsNullOrWhiteSpace(audit.ManagerName) ? audit.SiteManagerName : audit.ManagerName;
    }

    private HsAuditDetails FormDetails() => new(
        formType,
        ParseDate(formInspectionDate) ?? DateTimeOffset.UtcNow,
        formSiteManager.Trim(),
        formSafetyOfficer.Trim(),
        formSummary.Trim(),
        int.TryParse(formOperatives, out var operatives) ? operatives : null,
        formFurtherComments.Trim());

    private async Task SaveDetailsAsync()
    {
        await RunAsync(async () =>
        {
            await Commands.SendAsync(new UpdateHsAuditDetails(HsAuditId, FormDetails()), CancellationToken.None);
            await RefreshAfterWriteAsync();
        });
    }

    private async Task SaveItemsAsync(IReadOnlyList<HsAuditItemEntry> entries)
    {
        if (entries.Count == 0) return;
        await RunAsync(async () =>
        {
            var view = await Commands.SendAsync(new UpdateHsAuditItems(HsAuditId, entries), CancellationToken.None);
            Audits.Accept(view);
        });
    }

    private async Task IssueAsync()
    {
        await RunAsync(async () =>
        {
            var view = await Commands.SendAsync(new IssueHsAudit(HsAuditId), CancellationToken.None);
            issueOpen = false;
            Audits.Accept(view);
            await RefreshListAsync();
        });
    }

    private async Task CloseAsync()
    {
        await RunAsync(async () =>
        {
            await Commands.SendAsync(new CloseHsAudit(HsAuditId, managerName.Trim()), CancellationToken.None);
            closeOpen = false;
            await RefreshAfterWriteAsync();
            await RefreshListAsync();
        });
    }

    private async Task RunAsync(Func<Task> write)
    {
        busy = true;
        actionError = null;
        try { await write(); }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    // Post-write reload: swallow query failures (the toast already reported them) so a 502 on the
    // refetch can't take the page down after a successful write.
    private async Task RefreshAfterWriteAsync()
    {
        try { await Audits.RefreshViewAsync(HsAuditId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task RefreshListAsync()
    {
        try { await Audits.RefreshAsync(ProjectId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private void BackToTab() => Nav.NavigateTo($"/projects/{ProjectId}/hs");

    private static string SubtitleFor(HsAudit audit)
    {
        var officer = string.IsNullOrWhiteSpace(audit.SafetyOfficerName) ? "" : $" · {audit.SafetyOfficerName}";
        return $"Inspected {DateText(audit.InspectionDate)}{officer}";
    }

    private static HsAuditType ParseType(ChangeEventArgs e) =>
        int.TryParse(e.Value?.ToString(), out var value) ? (HsAuditType)value : HsAuditType.Routine;

    private static DateTimeOffset? ParseDate(string text) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var value)
            ? new DateTimeOffset(value, TimeSpan.Zero)
            : null;

    public void Dispose() => Audits.OnChanged -= OnAuditsChanged;
}
