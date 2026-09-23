using Jewel.JPMS.Features.Directory;

namespace Jewel.JPMS.Pages;

public partial class ComplianceRegister
{
    private string search = "";
    private string statusFilter = DirectoryComplianceFilter.All;
    private bool dataFailed;

    // Widened for the unified directory (2026-07-22): Admin, MD, FD and PM may browse.
    private bool CanAccess => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager);

    private bool IsLoaded => SubcontractorStore.IsLoaded && Compliance.Current is not null;

    private bool FiltersActive => !string.IsNullOrWhiteSpace(search) || statusFilter != DirectoryComplianceFilter.All;

    // Rebuilt when either source changes, not per render — the chips, the summary and the table
    // all read the same build.
    private IReadOnlyList<ComplianceRegisterRow> allRows = Array.Empty<ComplianceRegisterRow>();

    private void RebuildRows() =>
        allRows = IsLoaded
            ? ComplianceRegisterRows.Build(DirectoryCompanies(), Compliance.Current!, CompaniesOnSite)
            : Array.Empty<ComplianceRegisterRow>();

    // Tender-only prospects stay out, exactly as on the Directory page.
    private IReadOnlyList<Subcontractor> DirectoryCompanies() =>
        SubcontractorStore.All().Where(company => !company.IsProspect).ToList();

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        SubcontractorStore.OnChange += OnStoreChanged;
        Compliance.OnChanged += OnStoreChanged;
        _ = SubcontractorStore.All();
        _ = RefreshComplianceAsync();
        _ = RefreshOnSiteAsync();
        RebuildRows();
    }

    private async Task RefreshComplianceAsync()
    {
        try { await Compliance.RefreshAsync(CancellationToken.None); }
        catch { dataFailed = true; }
        RebuildRows();
        StateHasChanged();
    }

    public void Dispose()
    {
        SubcontractorStore.OnChange -= OnStoreChanged;
        Compliance.OnChanged -= OnStoreChanged;
    }

    private void OnStoreChanged()
    {
        RebuildRows();
        InvokeAsync(StateHasChanged);
    }

    private void Open(string subcontractorId) => Nav.NavigateTo($"/directory/{subcontractorId}");
}
