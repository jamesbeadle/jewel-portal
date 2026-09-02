namespace Jewel.JPMS.Components;

// Field plumbing: every edit raises OnChanged, which keeps the modal's packaging totals live.
public partial class WorkOrderForm
{
    private IEnumerable<Subcontractor> SortedSubcontractors =>
        (Subcontractors.Current ?? Array.Empty<Subcontractor>())
            .OrderBy(sub => sub.CompanyName, StringComparer.OrdinalIgnoreCase);

    private async Task SetSubcontractorAsync(string? value)
    {
        subcontractorId = value ?? "";
        assistantSupplierNote = null; // the user's own pick settles it
        await OnChanged.InvokeAsync();
    }

    private async Task SetSaveAsDraftAsync(ChangeEventArgs e)
    {
        saveAsDraft = e.Value is true;
        await OnChanged.InvokeAsync();
    }

    private async Task AddLineAsync()
    {
        lines.Add(new LineRow());
        await OnChanged.InvokeAsync();
    }

    private async Task RemoveLineAsync(int index)
    {
        lines.RemoveAt(index);
        await OnChanged.InvokeAsync();
    }

    // @bind:after on the title and scope inputs — the assistant task republishes the draft on
    // OnChanged, and those two fields travel in it, so their edits must raise it too.
    private Task NotifyChangedAsync() => OnChanged.InvokeAsync();

    private void SetLineCode(LineRow line, string? value) { line.CostCode = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineTitle(LineRow line, string? value) { line.Title = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineDescription(LineRow line, string? value) { line.Description = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineAmount(LineRow line, string? value) { line.AmountText = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineQuantity(LineRow line, string? value) { line.QuantityText = value ?? ""; RecalculateAmount(line); _ = OnChanged.InvokeAsync(); }
    private void SetLineUnit(LineRow line, string? value) { line.Unit = value ?? ""; _ = OnChanged.InvokeAsync(); }
    private void SetLineUnitCost(LineRow line, string? value) { line.UnitCostText = value ?? ""; RecalculateAmount(line); _ = OnChanged.InvokeAsync(); }
    private void SetDepositPercent(string? value) { depositPercentText = value ?? ""; _ = OnChanged.InvokeAsync(); }

    // Cost-centre picker options: typed-to-find, cached against the master list.
    private IReadOnlyList<SearchSelect.Option>? costCentreOptionsCache;
    private object? costCentreOptionsCacheKey;

    private IReadOnlyList<SearchSelect.Option> CostCentreOptions
    {
        get
        {
            var centres = CostCenters.Alphabetical;
            if (costCentreOptionsCache is null || !ReferenceEquals(costCentreOptionsCacheKey, centres))
            {
                costCentreOptionsCache = centres
                    .Select(centre => new SearchSelect.Option(centre.Code, $"{centre.Code} {centre.Name}"))
                    .ToList();
                costCentreOptionsCacheKey = centres;
            }
            return costCentreOptionsCache;
        }
    }

    // An order being edited can carry a code the master no longer offers — lead the list with the
    // stored value so the picker shows the code the line actually sits on.
    private IReadOnlyList<SearchSelect.Option> CostCentreOptionsFor(LineRow line)
    {
        var options = CostCentreOptions;
        if (line.CostCode == "" || options.Any(option => option.Value == line.CostCode)) return options;
        var master = options.FirstOrDefault(option =>
            string.Equals(option.Value, line.CostCode, StringComparison.OrdinalIgnoreCase));
        var withCurrent = new List<SearchSelect.Option>(options.Count + 1)
        {
            new(line.CostCode, master?.Label ?? $"{line.CostCode} (retired)")
        };
        withCurrent.AddRange(options.Where(option => !ReferenceEquals(option, master)));
        return withCurrent;
    }
}
