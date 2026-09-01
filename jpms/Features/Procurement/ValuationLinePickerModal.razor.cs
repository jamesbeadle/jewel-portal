using Jewel.JPMS.Features.CostCenters;

namespace Jewel.JPMS.Features.Procurement;

public partial class ValuationLinePickerModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    /// <summary>The trade each confirmed line defaults to — the package's own.</summary>
    [Parameter, EditorRequired] public string DefaultTrade { get; set; } = "";

    /// <summary>The host's in-flight flag while the confirm's command runs.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<BidPackageLineItemInput>> OnConfirm { get; set; }

    private IReadOnlyList<ValuationLineItem>? lines;   // null = no fetch has landed
    private bool loadFailed;
    private readonly HashSet<string> selection = new();
    private string search = "";
    private bool wasOpen;

    /// <summary>Each opening starts clean and reads the LIVE report — like the page's old picker.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen == wasOpen) return;
        wasOpen = IsOpen;
        if (!IsOpen) return;

        selection.Clear();
        search = "";
        lines = null;
        loadFailed = false;
        try
        {
            lines = await Queries.AskAsync(new ListValuationLinesForProject(ProjectId), CancellationToken.None);
            loadFailed = false;
        }
        catch { loadFailed = lines is null; }
    }

    private void OnSearchInput(ChangeEventArgs e) => search = e.Value?.ToString() ?? "";

    /// <summary>Declined report lines never become package scope.</summary>
    private IReadOnlyList<ValuationLineItem> SelectableLines => (lines ?? Array.Empty<ValuationLineItem>())
        .Where(line => line.LineType != ValuationLineType.Declined)
        .ToList();

    /// <summary>Search narrows the view only — ticks made before narrowing survive it.</summary>
    private IReadOnlyList<ValuationLineItem> FilteredLines
    {
        get
        {
            var query = search.Trim();
            if (query.Length == 0) return SelectableLines;
            return SelectableLines
                .Where(line => LineDescription(line).Contains(query, StringComparison.OrdinalIgnoreCase)
                    || line.CostCode.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || GroupLabel(line).Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private void ToggleLine(ValuationLineItem line)
    {
        if (string.IsNullOrWhiteSpace(line.CostCode)) return;
        if (!selection.Remove(line.ValuationLineItemId)) selection.Add(line.ValuationLineItemId);
    }

    private void ToggleGroup(IEnumerable<ValuationLineItem> group)
    {
        var selectable = group.Where(l => !string.IsNullOrWhiteSpace(l.CostCode)).Select(l => l.ValuationLineItemId).ToList();
        if (selectable.Count == 0) return;
        if (selectable.All(selection.Contains)) selection.ExceptWith(selectable);
        else selection.UnionWith(selectable);
    }

    private Task ConfirmAsync()
    {
        var inputs = SelectableLines
            .Where(line => selection.Contains(line.ValuationLineItemId) && !string.IsNullOrWhiteSpace(line.CostCode))
            .Select(line => new BidPackageLineItemInput(
                LineDescription(line),
                line.Unit.Trim(),
                line.Quantity,
                DefaultTrade,
                line.CostCode.Trim()))
            .ToList();
        return OnConfirm.InvokeAsync(inputs);
    }

    /// <summary>Section headers mirroring the report's blocks; contract works keep their section identity.</summary>
    private static string GroupLabel(ValuationLineItem line) => line.ElementType switch
    {
        ValuationElementType.ContractWorks =>
            string.IsNullOrWhiteSpace(line.SectionCode) && string.IsNullOrWhiteSpace(line.SectionName)
                ? "Contract works"
                : $"{line.SectionCode} — {line.SectionName}".Trim(' ', '—'),
        ValuationElementType.PcSum => "PC sums",
        ValuationElementType.Contingency => "Contingency",
        _ => "Variations",
    };

    /// <summary>
    /// The description the package line will carry. Variation lines lead with their V-number so
    /// the tenderer's schedule says which change the scope belongs to; blank descriptions fall
    /// back to the variation title or the section name rather than arriving empty.
    /// </summary>
    private static string LineDescription(ValuationLineItem line)
    {
        var description = line.Description.Trim();
        if (line.ElementType == ValuationElementType.Variation)
        {
            if (description.Length == 0) description = line.VariationTitle.Trim();
            var reference = line.VariationRef.Trim();
            if (reference.Length > 0 && description.Length > 0) description = $"{reference} — {description}";
            else if (reference.Length > 0) description = reference;
        }
        return description.Length == 0 ? line.SectionName.Trim() : description;
    }

    private static string? LineTypeBadge(ValuationLineItem line) => line.LineType switch
    {
        ValuationLineType.ProvisionalSum => "PS",
        ValuationLineType.Omit => "Omit",
        ValuationLineType.Tbc => "TBC",
        _ => null,
    };

    private string CostCentreLabel(string code)
    {
        var centre = CostCenters.Alphabetical.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        return centre is null ? code : $"{centre.Code} — {centre.Name}";
    }
}
