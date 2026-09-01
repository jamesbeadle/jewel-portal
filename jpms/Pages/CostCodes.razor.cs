using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class CostCodes
{
    private enum CostCodesTab { Ours, XeroSites, XeroCostCodes }

    // Session checked and the user signed in. This is NOT "the data is here" — keeping the two
    // apart is what lets the page show its chrome at once and hold the table until the list lands.
    private bool sessionReady;
    private bool busy;
    private string? loadError;
    private string? formError;
    // Nullable on purpose. "No cost codes yet" is a real answer, so "not fetched" needs to be a
    // distinct state — otherwise the empty state is shown before anything was asked.
    private IReadOnlyList<CostCenter>? costCodes;
    private bool showRetired;

    private CostCodesTab activeTab = CostCodesTab.Ours;
    private bool xeroRefreshing;
    // The project list is only fetched once the sites tab needs it (its "Linked project" column)
    // — someone in to edit the master shouldn't cost a projects read.
    private bool projectsRequested;
    private bool projectsFailed;

    private IReadOnlyList<CostCenter> AllCostCodes => costCodes ?? Array.Empty<CostCenter>();

    // A failed fetch opens the gate too, or the jewel pulses forever; the red bar above says why.
    private bool CostCodesReady => costCodes is not null || loadError is not null;

    // The sites tab reads the snapshot AND the project list, so it reveals in one piece; the cost
    // codes tab only needs the snapshot (the master list is already loaded for the first tab).
    // Snapshot() itself starts the lazy fetch — the first read of a Xero tab kicks it off, so
    // opening the page just to edit the master never costs a Xero call.
    private bool XeroTabReady =>
        XeroTracking.Snapshot() is not null
        && (activeTab != CostCodesTab.XeroSites || Projects.Current is not null || projectsFailed);

    private IReadOnlyList<CostCenter> VisibleCostCodes =>
        showRetired ? AllCostCodes : AllCostCodes.Where(c => c.IsActive).ToList();

    private bool CanManage => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.QuantitySurveyor);

    private bool adding;
    private bool editing;
    private CostCenter? editTarget;
    private string formCode = "";
    private string formName = "";
    private string formSortOrder = "";

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        CostCenters.OnChange += StateHasChanged;
        XeroTracking.OnChange += StateHasChanged;
        Projects.OnChanged += StateHasChanged;
        sessionReady = true;
        // Paint the chrome before the fetch: Blazor re-renders OnInitializedAsync only at its
        // FIRST await, which has already passed, so without this the page waits on the list.
        StateHasChanged();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try { costCodes = await CostCenters.ListAllAsync(); loadError = null; }
        catch { loadError = "Couldn't load the cost codes. Please try again."; }
    }

    private void SwitchTab(CostCodesTab tab)
    {
        activeTab = tab;
        if (tab == CostCodesTab.XeroSites) EnsureProjectsRequested();
    }

    private static string TabCss(bool active) =>
        (active ? "bg-content text-surface" : "bg-surface text-content-muted hover:text-content")
        + " text-sm font-medium px-4 py-1.5 transition-colors";

    private void EnsureProjectsRequested()
    {
        if (projectsRequested) return;
        projectsRequested = true;
        _ = LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        try { await Projects.RefreshAsync(CancellationToken.None); projectsFailed = false; }
        catch { projectsFailed = true; }
        StateHasChanged();
    }

    private async Task ForceRefreshXeroAsync()
    {
        if (xeroRefreshing) return;
        try
        {
            xeroRefreshing = true;
            await XeroTracking.RefreshAsync(force: true);
        }
        catch { /* the error toast already carries the reference and the detail */ }
        finally { xeroRefreshing = false; }
    }

    private static string FetchedText(XeroTrackingCategoriesSnapshot snapshot) =>
        snapshot.FetchedAtUtc is { } fetched ? fetched.ToLocalTime().ToString("HH:mm") : "—";

    private static IReadOnlyList<XeroTrackingOption> SortedOptions(XeroTrackingCategory category) =>
        category.Options.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Projects whose Xero site mapping names this option — same trim/case tolerance as the write-back's match.</summary>
    private IReadOnlyList<Project> ProjectsMappedTo(string optionName) =>
        (Projects.Current ?? Array.Empty<Project>())
            .Where(p => !string.IsNullOrWhiteSpace(p.XeroSiteName)
                        && string.Equals(p.XeroSiteName!.Trim(), optionName.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <summary>Projects with a Xero site set that matches none of the category's options — the write-back will refuse these.</summary>
    private IReadOnlyList<Project> UnmatchedSiteMappings(XeroTrackingCategory category) =>
        (Projects.Current ?? Array.Empty<Project>())
            .Where(p => !string.IsNullOrWhiteSpace(p.XeroSiteName)
                        && !category.Options.Any(o =>
                            string.Equals(o.Name.Trim(), p.XeroSiteName!.Trim(), StringComparison.OrdinalIgnoreCase)))
            .InWorkOrder()
            .ToList();

    /// <summary>The system code (by Code) a Xero option corresponds to — the write-back stamps CostCenter.Code as the option name.</summary>
    private CostCenter? SystemCodeFor(string optionName) =>
        AllCostCodes.FirstOrDefault(c => string.Equals(c.Code.Trim(), optionName.Trim(), StringComparison.OrdinalIgnoreCase));

    private IReadOnlyList<CostCenter> SystemCodesWithoutXeroOption(XeroTrackingCategory category) =>
        AllCostCodes
            .Where(c => c.IsActive && !category.Options.Any(o =>
                string.Equals(o.Name.Trim(), c.Code.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToList();

    private void OpenAdd()
    {
        formError = null;
        formCode = formName = formSortOrder = "";
        adding = true;
    }

    private void OpenEdit(CostCenter costCode)
    {
        formError = null;
        editTarget = costCode;
        formCode = costCode.Code;
        formName = costCode.Name;
        formSortOrder = costCode.SortOrder.ToString();
        editing = true;
    }

    private async Task AddAsync()
    {
        if (busy) return;
        formError = null;
        if (string.IsNullOrWhiteSpace(formCode)) { formError = "A code is required."; return; }
        if (string.IsNullOrWhiteSpace(formName)) { formError = "A name is required."; return; }
        if (!TryParseSortOrder(out var sortOrder)) return;
        try
        {
            busy = true;
            await CostCenters.AddAsync(new AddCostCenter(formCode.Trim(), formName.Trim(), sortOrder));
            adding = false;
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { formError = ex.Message; }
        catch { formError = "Couldn't add the cost code. Please try again."; }
        finally { busy = false; }
    }

    private async Task SaveEditAsync()
    {
        if (busy || editTarget is null) return;
        formError = null;
        if (string.IsNullOrWhiteSpace(formCode)) { formError = "A code is required."; return; }
        if (string.IsNullOrWhiteSpace(formName)) { formError = "A name is required."; return; }
        // Unlike Add, a blank sort order has no "append" meaning here — require it.
        if (string.IsNullOrWhiteSpace(formSortOrder)) { formError = "A sort order is required."; return; }
        if (!TryParseSortOrder(out var sortOrder)) return;
        try
        {
            busy = true;
            await CostCenters.ReviseAsync(new ReviseCostCenter(
                editTarget.CostCenterId,
                formCode.Trim(),
                formName.Trim(),
                sortOrder,
                editTarget.IsActive));
            editing = false;
            await ReloadAsync();
        }
        catch (CommandFailedException ex) { formError = ex.Message; }
        catch { formError = "Couldn't save the changes. Please try again."; }
        finally { busy = false; }
    }

    private async Task ToggleActiveAsync(CostCenter costCode)
    {
        if (busy) return;
        try
        {
            busy = true;
            await CostCenters.ReviseAsync(new ReviseCostCenter(
                costCode.CostCenterId,
                costCode.Code,
                costCode.Name,
                costCode.SortOrder,
                !costCode.IsActive));
            await ReloadAsync();
        }
        catch { loadError = "Couldn't update the cost code. Please try again."; }
        finally { busy = false; }
    }

    private ExcelWorkbook? BuildExportWorkbook(bool includeRetired)
    {
        // "Include retired" exports the whole master list even while the table hides
        // retired codes — the Status column tells them apart.
        var costCodesToExport = includeRetired ? AllCostCodes : VisibleCostCodes;
        if (costCodesToExport.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Cost codes",
            new ExcelColumn("Code"),
            new ExcelColumn("Name"),
            new ExcelColumn("Sort order", ExcelFormat.Integer),
            new ExcelColumn("Status"));

        foreach (var costCode in costCodesToExport)
        {
            sheet.AddRow(
                costCode.Code,
                costCode.Name,
                costCode.SortOrder,
                costCode.IsActive ? "Active" : "Retired");
        }
        return workbook;
    }

    private bool TryParseSortOrder(out int sortOrder)
    {
        sortOrder = 0;
        if (string.IsNullOrWhiteSpace(formSortOrder)) return true;
        if (int.TryParse(formSortOrder, out sortOrder) && sortOrder >= 0) return true;
        formError = "The sort order must be a non-negative number.";
        return false;
    }

    public void Dispose()
    {
        CostCenters.OnChange -= StateHasChanged;
        XeroTracking.OnChange -= StateHasChanged;
        Projects.OnChanged -= StateHasChanged;
    }
}
