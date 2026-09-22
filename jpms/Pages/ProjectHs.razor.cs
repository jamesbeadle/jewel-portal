using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Pages;

public partial class ProjectHs
{
    private const string AuditsPane = "audits";
    private const string ActionsPane = "actions";
    private const string RegisterPane = "register";

    [Parameter] public string ProjectId { get; set; } = "";
    [SupplyParameterFromQuery(Name = "view")] public string? ViewKey { get; set; }

    private bool dataFailed;
    private bool busy;
    private string? actionError;

    private bool newAuditOpen;
    private HsAuditType newType = HsAuditType.Routine;
    private string newInspectionDate = DateTime.Today.ToString("yyyy-MM-dd");
    private string newSafetyOfficer = "";
    private string newSiteManager = "";

    private bool logRecordOpen;
    private HsRecordKind logKind = HsRecordKind.Observation;
    private HsSeverity logSeverity = HsSeverity.Low;
    private string logOwner = "";
    private string logDue = "";
    private string logSummary = "";

    private string Pane => ViewKey switch
    {
        ActionsPane => ActionsPane,
        RegisterPane => RegisterPane,
        _ => AuditsPane
    };

    private IReadOnlyList<TabItem> Panes => new[]
    {
        new TabItem(AuditsPane, "Audits", $"/projects/{ProjectId}/hs", AuditRows.Count),
        new TabItem(ActionsPane, "Actions", $"/projects/{ProjectId}/hs?view={ActionsPane}", OpenActionCount),
        new TabItem(RegisterPane, "Register", $"/projects/{ProjectId}/hs?view={RegisterPane}")
    };

    private IReadOnlyList<HsAudit> AuditRows => Audits.AuditsFor(ProjectId) ?? Array.Empty<HsAudit>();

    private IReadOnlyList<HsRecord> ProjectRecords =>
        Records.Current is null ? Array.Empty<HsRecord>() : Register.ForProject(ProjectId);

    private IReadOnlyList<HsRecord> ActionRows =>
        ProjectRecords.Where(record => record.Kind == HsRecordKind.CorrectiveAction).OrderBy(SortRank).ThenBy(record => record.DueAt ?? DateTimeOffset.MaxValue).ToList();

    private IReadOnlyList<HsRecord> RegisterRows =>
        ProjectRecords.Where(record => record.Kind != HsRecordKind.CorrectiveAction).OrderByDescending(record => record.RaisedAt).ToList();

    private int? OpenActionCount =>
        Records.Current is null ? null : ActionRows.Count(record => record.Status != HsStatus.Closed);

    private bool LogFormReady => !string.IsNullOrWhiteSpace(logSummary) && !string.IsNullOrWhiteSpace(logOwner);

    protected override async Task OnInitializedAsync()
    {
        Audits.OnChanged += StateHasChanged;
        Records.OnChanged += StateHasChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        await LoadAsync();
    }

    private string loadedForProjectId = "";

    protected override async Task OnParametersSetAsync()
    {
        if (loadedForProjectId == "" || loadedForProjectId == ProjectId) return;
        dataFailed = false;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loadedForProjectId = ProjectId;
        try
        {
            await Audits.RefreshAsync(ProjectId, CancellationToken.None);
            await Records.RefreshAsync(CancellationToken.None);
        }
        catch { dataFailed = true; }
    }

    private void OpenAudit(HsAudit audit) => Nav.NavigateTo($"/projects/{ProjectId}/hs/audits/{audit.HsAuditId}");

    private async Task CreateAuditAsync()
    {
        await RunAsync(async () =>
        {
            var details = new HsAuditDetails(newType, ParseDate(newInspectionDate) ?? DateTimeOffset.UtcNow,
                newSiteManager.Trim(), newSafetyOfficer.Trim(), "", null, "");
            var audit = await Commands.SendAsync(new CreateHsAudit(ProjectId, details), CancellationToken.None);
            newAuditOpen = false;
            OpenAudit(audit);
        });
    }

    private async Task LogRecordAsync()
    {
        await RunAsync(async () =>
        {
            await Commands.SendAsync(
                new LogHsRecord(ProjectId, logKind, logSummary.Trim(), logSeverity, "", ParseDate(logDue), logOwner.Trim()),
                CancellationToken.None);
            logRecordOpen = false;
            logSummary = ""; logOwner = ""; logDue = "";
            await RefreshRecordsAsync();
        });
    }

    private async Task SetStatusAsync(HsRecord record, ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out var statusValue)) return;
        var status = (HsStatus)statusValue;
        if (status == record.Status) return;
        await RunAsync(async () =>
        {
            await Commands.SendAsync(
                new UpdateHsRecord(record.HsRecordId, record.Summary, record.Severity, status, record.AssignedToEmail, record.DueAt, record.AssignedToName),
                CancellationToken.None);
            await RefreshRecordsAsync();
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

    private async Task RefreshRecordsAsync()
    {
        try { await Records.RefreshAsync(CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private static int SortRank(HsRecord record) => record.Status == HsStatus.Closed ? 1 : 0;

    private static string StatusLabel(HsStatus status) => status switch
    {
        HsStatus.InProgress => "In progress",
        _ => status.ToString()
    };

    private static HsAuditType ParseType(ChangeEventArgs e) =>
        int.TryParse(e.Value?.ToString(), out var value) ? (HsAuditType)value : HsAuditType.Routine;

    private static HsRecordKind ParseKind(ChangeEventArgs e) =>
        int.TryParse(e.Value?.ToString(), out var value) ? (HsRecordKind)value : HsRecordKind.Observation;

    private static HsSeverity ParseSeverity(ChangeEventArgs e) =>
        int.TryParse(e.Value?.ToString(), out var value) ? (HsSeverity)value : HsSeverity.Low;

    private static DateTimeOffset? ParseDate(string text) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var value)
            ? new DateTimeOffset(value, TimeSpan.Zero)
            : null;

    public void Dispose()
    {
        Audits.OnChanged -= StateHasChanged;
        Records.OnChanged -= StateHasChanged;
    }
}
