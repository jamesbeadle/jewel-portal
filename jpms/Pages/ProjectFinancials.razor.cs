using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectFinancials
{
    [Parameter] public string ProjectId { get; set; } = "";

    private bool isLoaded;
    private string? actionError;

    // Computed here rather than in an inline @{ } block: the banner sits inside an else { } code
    // block where Razor (RZ1010) forbids re-entering code with @{.
    private decimal PendingLabourTotal => Summary.Current(ProjectId).Sum(row => row.PendingLabourCost);

    // A clicked-through figure: its heading and the cost codes behind it (one for an
    // individual row, all members for a roll-up).
    private sealed record SelectedLine(string Heading, IReadOnlyList<string> CostCodes);

    private SelectedLine? selectedSales;
    private SelectedLine? selectedWo;
    private SelectedLine? selectedCostOfSales;
    private SelectedLine? selectedReport;

    // Roll-up naming dialog state; non-null pendingGroupCodes means the dialog is open.
    // pendingReplaceGroupIds carries any selected roll-ups being merged into the new group.
    private IReadOnlyList<string>? pendingGroupCodes;
    private IReadOnlyList<string> pendingReplaceGroupIds = Array.Empty<string>();
    private string groupName = "";
    private string? groupDialogError;
    private bool isCreatingGroup;

    // Committed work-order value per cost code — order line totals, from the same store the
    // Work Orders tab renders. Lines without a cost code can't land on a Financials row, so
    // they're excluded here (they surface on the Work Orders tab instead).
    private IReadOnlyDictionary<string, decimal> WoCommittedByCode =>
        ProjectDrawdown.CommittedByCostCode(WorkOrders.Current(ProjectId));

    private Task RetrySummaryAsync() => Summary.RefreshAsync(ProjectId, CancellationToken.None);

    // The computed package rows, surfaced by the Packages section whenever it (re)loads —
    // shown as first-class lines in the main table so the total covers the whole project.
    private IReadOnlyList<PackageReconciliationRow> packageRows = Array.Empty<PackageReconciliationRow>();
    private bool packageRowsSeen;

    // Package membership also drives the summary's netted (PackagedX) figures, so any
    // package change re-pulls the summary to keep the centre rows and package rows in
    // step. The first load after page init is the section's initial fetch — the summary
    // is already loading, so skip the extra round-trip.
    private async Task HandlePackageRowsChangedAsync(IReadOnlyList<PackageReconciliationRow> rows)
    {
        packageRows = rows;
        if (!packageRowsSeen)
        {
            packageRowsSeen = true;
            return;
        }
        await Summary.RefreshAsync(ProjectId, CancellationToken.None);
    }

    // A manual work order was raised from the Packages section: the WO Committed column
    // reads from the work-orders store, so re-pull it (the summary follows via the
    // package reload's OnRowsChanged).
    private Task HandleOrdersChangedAsync() => WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);

    private void OpenSalesLinesModal((string Heading, IReadOnlyList<string> CostCodes) line) =>
        selectedSales = new SelectedLine(line.Heading, line.CostCodes);

    private void CloseSalesLinesModal() => selectedSales = null;

    private void OpenWorkOrdersModal((string Heading, IReadOnlyList<string> CostCodes) line) =>
        selectedWo = new SelectedLine(line.Heading, line.CostCodes);

    private void CloseWorkOrdersModal() => selectedWo = null;

    private void OpenCostOfSalesModal((string Heading, IReadOnlyList<string> CostCodes) line) =>
        selectedCostOfSales = new SelectedLine(line.Heading, line.CostCodes);

    private void CloseCostOfSalesModal() => selectedCostOfSales = null;

    private void OpenReconciliationModal((string Heading, IReadOnlyList<string> CostCodes) line) =>
        selectedReport = new SelectedLine(line.Heading, line.CostCodes);

    private void CloseReconciliationModal() => selectedReport = null;

    // An invoice was reallocated to another cost centre from inside the modal:
    // close it and re-pull the summary so the table reflects the move.
    private async Task HandleInvoiceMovedAsync()
    {
        selectedCostOfSales = null;
        await Summary.RefreshAsync(ProjectId, CancellationToken.None);
    }

    // An invoice's work-order link changed inside the modal: re-pull the summary so the
    // non-WO cost of sales and drawdown columns update behind it. The modal stays open.
    private Task HandleWorkOrderLinkChangedAsync() => Summary.RefreshAsync(ProjectId, CancellationToken.None);

    // A valuation line was recoded to another centre inside the sales-lines modal: the
    // store already re-pulled the lines, so re-pull the summary too and the Contract
    // Sales Value figures move with the line. The modal stays open — the recoded row
    // simply drops out of its list, so several lines can be fixed in one sitting.
    private Task HandleSalesLineRecodedAsync() => Summary.RefreshAsync(ProjectId, CancellationToken.None);

    // A line's lock button was clicked: apply the finalisation state to every cost code
    // behind the line, then re-pull the summary so drawdown / profit-loss move together.
    private async Task HandleFinalisationToggledAsync((IReadOnlyList<string> CostCodes, bool Finalise) change)
    {
        actionError = null;
        try
        {
            foreach (var costCode in change.CostCodes)
                await Commands.SendAsync(
                    new SetCostCentreFinalisation(ProjectId, costCode, change.Finalise), CancellationToken.None);
        }
        catch (CommandFailedException ex)
        {
            actionError = $"Could not update the lock — {ex.Message}";
        }
        await Summary.RefreshAsync(ProjectId, CancellationToken.None);
    }

    // Selected roll-ups are expanded to their member centres and dissolved on create —
    // this is how a third centre joins an existing group, or two groups merge, without
    // ungrouping first. The dialog prefills the first selected group's name.
    private void OpenGroupDialog((IReadOnlyList<string> CostCodes, IReadOnlyList<string> GroupIds) selection)
    {
        var selectedGroups = Groups.Current(ProjectId)
            .Where(group => selection.GroupIds.Contains(group.CostCentreGroupId, StringComparer.OrdinalIgnoreCase))
            .ToList();

        pendingGroupCodes = selection.CostCodes
            .Concat(selectedGroups.SelectMany(group => group.CostCodes))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(costCode => costCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        pendingReplaceGroupIds = selectedGroups.Select(group => group.CostCentreGroupId).ToList();
        groupName = selectedGroups.FirstOrDefault()?.Name ?? "";
        groupDialogError = null;
    }

    private void CloseGroupDialog() => pendingGroupCodes = null;

    private async Task CreateGroupAsync()
    {
        if (pendingGroupCodes is null || string.IsNullOrWhiteSpace(groupName)) return;
        isCreatingGroup = true;
        groupDialogError = null;
        try
        {
            await Commands.SendAsync(
                new CreateCostCentreGroup(ProjectId, groupName.Trim(), pendingGroupCodes,
                    pendingReplaceGroupIds.Count > 0 ? pendingReplaceGroupIds : null), CancellationToken.None);
            pendingGroupCodes = null;
            await Groups.RefreshAsync(ProjectId, CancellationToken.None);
        }
        catch (CommandFailedException ex)
        {
            groupDialogError = ex.Message;
        }
        finally
        {
            isCreatingGroup = false;
        }
    }

    private async Task HandleUngroupAsync(string groupId)
    {
        actionError = null;
        try
        {
            await Commands.SendAsync(new RemoveCostCentreGroup(ProjectId, groupId), CancellationToken.None);
        }
        catch (CommandFailedException ex)
        {
            actionError = $"Could not ungroup — {ex.Message}";
        }
        await Groups.RefreshAsync(ProjectId, CancellationToken.None);
    }

    // A line's Cost % Complete was edited: persist it to every cost code behind the line
    // (one for an individual row, all members for a roll-up), then re-pull the summary so
    // the table and its weighted totals reflect the saved values.
    private async Task HandleCostCompletionChangedAsync((IReadOnlyList<string> CostCodes, decimal Percent) edit)
    {
        actionError = null;
        try
        {
            foreach (var costCode in edit.CostCodes)
                await Commands.SendAsync(
                    new SetCostCentreCostCompletion(ProjectId, costCode, edit.Percent), CancellationToken.None);
        }
        catch (CommandFailedException)
        {
            actionError = "Could not save the cost % complete — please try again.";
        }
        await Summary.RefreshAsync(ProjectId, CancellationToken.None);
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        CostCenters.OnChanged += StateHasChanged;
        Summary.OnChanged += StateHasChanged;
        WorkOrders.OnChanged += StateHasChanged;
        ValuationLines.OnChanged += StateHasChanged;
        Groups.OnChanged += StateHasChanged;
        // Refresh once per tab entry (stale-while-revalidate, per the front-end
        // data-loading convention) — cached figures render immediately, then update.
        _ = Summary.RefreshAsync(ProjectId, CancellationToken.None);
        _ = WorkOrders.RefreshAsync(ProjectId, CancellationToken.None);
        _ = ValuationLines.RefreshAsync(ProjectId, CancellationToken.None);
        _ = Groups.RefreshAsync(ProjectId, CancellationToken.None);
        await CostCenters.RefreshAsync(CancellationToken.None);
        isLoaded = true;
    }

    public void Dispose()
    {
        CostCenters.OnChanged -= StateHasChanged;
        Summary.OnChanged -= StateHasChanged;
        WorkOrders.OnChanged -= StateHasChanged;
        ValuationLines.OnChanged -= StateHasChanged;
        Groups.OnChanged -= StateHasChanged;
    }
}
