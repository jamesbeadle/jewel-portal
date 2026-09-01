using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Features.Procurement;

/// <summary>What one Save hands back: the specification summary and the kept, trimmed line
/// schedule — blank-description rows already dropped, every line carrying its cost code.</summary>
public sealed record BidPackageDetailsDraft(
    string SpecificationSummary, IReadOnlyList<BidPackageLineItemInput> LineItems);

public partial class PackageDetailsEditorModal
{
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>The package's current summary and schedule — the drafts seed from these each
    /// time the dialog opens.</summary>
    [Parameter] public string SpecificationSummary { get; set; } = "";
    [Parameter] public IReadOnlyList<BidPackageLineItem> LineItems { get; set; } = Array.Empty<BidPackageLineItem>();

    /// <summary>The host's in-flight flag while its two save commands run.</summary>
    [Parameter] public bool Busy { get; set; }

    /// <summary>The host's save failure, shown in the dialog — everything typed stays put.</summary>
    [Parameter] public string? Error { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<BidPackageDetailsDraft> OnSave { get; set; }

    private string specDraft = "";
    private List<LineDraft> lineDrafts = new();
    private string? validationError;
    private bool wasOpen;

    private sealed class LineDraft
    {
        public string Trade { get; set; } = "";
        public string Description { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Quantity { get; set; }
        public string CostCode { get; set; } = "";
    }

    protected override void OnParametersSet()
    {
        if (IsOpen == wasOpen) return;
        wasOpen = IsOpen;
        if (!IsOpen) { lineDrafts.Clear(); return; }
        validationError = null;
        specDraft = SpecificationSummary;
        lineDrafts = LineItems
            .Select(item => new LineDraft { Trade = item.Trade, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity, CostCode = item.CostCode })
            .ToList();
        if (lineDrafts.Count == 0) lineDrafts.Add(new LineDraft());
    }

    private async Task SaveAsync()
    {
        if (Busy) return;
        var kept = lineDrafts
            .Where(draft => !string.IsNullOrWhiteSpace(draft.Description))
            .ToList();
        // Every line put out to tender must know its cost-centre home.
        if (kept.Any(draft => string.IsNullOrWhiteSpace(draft.CostCode)))
        {
            validationError = "Every line item needs a cost code — pick a cost centre for each line before saving.";
            return;
        }
        validationError = null;
        var lines = kept
            .Select(draft => new BidPackageLineItemInput(
                draft.Description.Trim(),
                (draft.Unit ?? "").Trim(),
                draft.Quantity,
                (draft.Trade ?? "").Trim(),
                draft.CostCode.Trim()))
            .ToList();
        await OnSave.InvokeAsync(new BidPackageDetailsDraft(specDraft.Trim(), lines));
    }
}
