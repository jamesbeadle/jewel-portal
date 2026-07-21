using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers whether a user last viewed a project's Work Orders tab grouped
/// by cost centre or by supplier (per browser, per user), so the screen
/// reopens in the grouping they work in — an accountant who records against
/// suppliers lands straight back on the supplier view. Stored value is
/// "cost-centre" or "supplier".
/// </summary>
public sealed class WorkOrderGroupingStorage
{
    private const string SupplierValue = "supplier";
    private const string CostCentreValue = "cost-centre";

    private const string StorageKeyPrefix = "jpms.workOrderGrouping";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public WorkOrderGroupingStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<bool> ReadGroupBySupplierAsync(string email)
    {
        try { return await js.InvokeAsync<string?>(GetItem, StorageKeyFor(email)) == SupplierValue; }
        catch { return false; }
    }

    public async Task WriteAsync(string email, bool groupBySupplier)
    {
        try { await js.InvokeVoidAsync(SetItem, StorageKeyFor(email), groupBySupplier ? SupplierValue : CostCentreValue); }
        catch { }
    }

    private static string StorageKeyFor(string email) =>
        $"{StorageKeyPrefix}.{email.Trim().ToLowerInvariant()}";
}
