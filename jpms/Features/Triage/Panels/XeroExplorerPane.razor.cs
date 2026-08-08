using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Features.Triage.Panels;

public partial class XeroExplorerPane : IDisposable
{
    private const int ResultCap = 100;

    private string search = "";
    private string typeFilter = "";
    private XeroTransaction? openTransaction;
    // A failed read must open the gate: without this the pane would pulse forever on a dead read.
    private bool loadFailed;

    protected override async Task OnInitializedAsync()
    {
        Xero.OnChange += StoreChanged;
        try { await Xero.RefreshAsync(); }
        catch { loadFailed = true; }
    }

    private IReadOnlyList<XeroTransaction> FilteredTransactions
    {
        get
        {
            var transactions = Xero.Snapshot()?.Transactions ?? (IReadOnlyList<XeroTransaction>)Array.Empty<XeroTransaction>();
            var needle = search.Trim();
            return transactions
                .Where(transaction => typeFilter == "" || transaction.Type == typeFilter)
                .Where(transaction => needle.Length == 0 || Matches(transaction, needle))
                .Take(ResultCap)
                .ToList();
        }
    }

    private static bool Matches(XeroTransaction transaction, string needle) =>
        (transaction.ContactName?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
        || (transaction.Number?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
        || (transaction.Reference?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);

    internal static string TypeLabel(string type) => type switch
    {
        "ACCPAY" => "Purchase invoice",
        "ACCPAYCREDIT" => "Supplier credit note",
        "ACCREC" => "Sales invoice",
        _ => type
    };

    private void StoreChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Xero.OnChange -= StoreChanged;
}
