using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Pages;

public partial class WeeklyCashflow
{
    // The Xero snapshots become seeds through WeeklyCashflowSeeding — the same mapping the
    // connector's get_weekly_cashflow_grid uses, so the two can never read a bill differently.
    // An excluded seed never reaches the maths: it renders struck-through at the foot of its
    // band instead, so the money is visibly parked rather than silently dropped.
    private WeeklyCashflowView BuildView()
    {
        var plan = Plan.Current!;
        var billSeeds = PayablesSnapshot!.Bills.Select(WeeklyCashflowSeeding.FromBill);
        var invoiceSeeds = ReceivablesSnapshot!.Invoices.Select(WeeklyCashflowSeeding.FromInvoice);
        var (counted, excluded) = WeeklyCashflowSeeding.Split(billSeeds.Concat(invoiceSeeds), plan.Exclusions);

        excludedSeeds.Clear();
        excludedSeeds.AddRange(excluded);

        return WeeklyCashflowMaths.Build(
            today,
            counted,
            plan.Items,
            plan.Placements,
            IsDirector && BankReady ? BankSnapshot!.TotalCash : null);
    }

    private void ToggleBand(WeeklyCashflowBand band)
    {
        if (!collapsedBands.Remove(band)) collapsedBands.Add(band);
    }

    private void ToggleGroup(string supplierGroupId)
    {
        if (!expandedGroups.Remove(supplierGroupId)) expandedGroups.Add(supplierGroupId);
    }

    private string? ExcludedByFor(string placementKey) =>
        Plan.Current?.Exclusions.FirstOrDefault(exclusion => exclusion.PlacementKey == placementKey)?.ExcludedByEmail;

    /// <summary>The supplier groups as they land on this grid — GroupSlice.For's rule (shared with
    /// the export, so the two fold the same bills into the same lines), fed the current plan.</summary>
    private IReadOnlyList<GroupSlice> GroupSlicesFor(WeeklyCashflowView view) =>
        GroupSlice.For(view, Plan.Current?.SupplierGroups ?? Array.Empty<WeeklyCashflowSupplierGroup>());
}
