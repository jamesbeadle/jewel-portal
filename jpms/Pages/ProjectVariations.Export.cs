using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariations
{
    // ---- Excel export ----------------------------------------------------------------------
    // One Variations sheet — the current view (following the search); "Include entire register"
    // from the export menu overrides the search and takes every row.

    private ExcelWorkbook? BuildExportWorkbook(bool includeEntireRegister)
    {
        var workbook = new ExcelWorkbook();

        var variationRows = includeEntireRegister ? (IReadOnlyList<VariationOrder>)Rows : FilteredRows;
        if (variationRows.Count > 0)
        {
            var sheet = workbook.AddSheet("Variations",
                new ExcelColumn("Ref"),
                new ExcelColumn("Title"),
                new ExcelColumn("Request"),
                new ExcelColumn("Status"),
                new ExcelColumn("Value", ExcelFormat.Currency),
                new ExcelColumn("Issued", ExcelFormat.Date),
                new ExcelColumn("Approved", ExcelFormat.Date),
                new ExcelColumn("Created", ExcelFormat.Date),
                new ExcelColumn("Work order"));

            foreach (var order in variationRows)
            {
                var sourceRequest = RequestFor(order);
                var issued = IssuedWorkOrdersFor(order);
                sheet.AddRow(
                    RowReference(order),
                    order.Title,
                    sourceRequest is null ? null : RefLabel(sourceRequest),
                    VariationStatusLabel(order),
                    RowValue(order),
                    order.IssuedAt?.LocalDateTime,
                    order.Status == VariationOrderStatus.Approved ? order.ApprovedAt?.LocalDateTime : null,
                    order.CreatedAt.LocalDateTime,
                    issued.Count == 0 ? null : string.Join(", ", issued.Select(wo => $"WO-{wo.Number:0000}")));
            }
        }

        return workbook.Sheets.Count == 0 ? null : workbook;
    }

    private async Task LoadVariationsAsync()
    {
        try
        {
            orders = await Variations.ListForProjectAsync(ProjectId);
            variationRequests = await Variations.ListVariationRequestsForProjectAsync(ProjectId);
            Rows = orders.OrderByDescending(order => order.Number).ToList();
            variationsError = null;
        }
        catch { variationsError = "Couldn't load variations. Please try again."; }
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        RequestRegister.OnChange += StateHasChanged;
        Activity.OnChanged += StateHasChanged;
        // Refresh on entry (stale-while-revalidate): cached data renders immediately, then
        // updates when the background reload lands.
        RequestRegister.Refresh(ProjectId); // The Request column + search read the register.
        Procurement.Refresh(ProjectId);     // Background revalidation of work orders (issued-WO column).
        Activity.Refresh(ProjectId);        // Activity badges land in the background — absent until then.
        await LoadVariationsAsync();
        isLoaded = true;
    }

    public void Dispose()
    {
        RequestRegister.OnChange -= StateHasChanged;
        Activity.OnChanged -= StateHasChanged;
    }
}
