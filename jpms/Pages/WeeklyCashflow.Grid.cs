using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Features.WeeklyCashflow;

namespace Jewel.JPMS.Pages;

public partial class WeeklyCashflow
{
    private WeeklyCashflowView BuildView()
    {
        var seeds = new List<WeeklyCashflowSeed>();
        excludedSeeds.Clear();
        var excludedKeys = ExcludedKeys();

        // An excluded seed never reaches the maths — it renders struck-through at the foot of
        // its band instead, so the money is visibly parked rather than silently dropped.
        void Sort(WeeklyCashflowSeed seed)
        {
            if (excludedKeys.Contains(seed.PlacementKey)) excludedSeeds.Add(seed);
            else seeds.Add(seed);
        }

        foreach (var bill in PayablesSnapshot!.Bills)
        {
            Sort(new WeeklyCashflowSeed(
                WeeklyCashflowMaths.BillKeyFor(bill.InvoiceId),
                WeeklyCashflowBand.SupplierBills,
                string.IsNullOrWhiteSpace(bill.ContactName) ? "(no supplier)" : bill.ContactName!.Trim(),
                BillDetail(bill),
                AgedPayablesMaths.SignedAmountDue(bill),
                AsDate(bill.DueDate ?? bill.Date),
                AsDate(bill.PlannedPaymentDate)));
        }

        foreach (var invoice in ReceivablesSnapshot!.Invoices)
        {
            Sort(new WeeklyCashflowSeed(
                WeeklyCashflowMaths.ReceiptKeyFor(invoice.InvoiceId),
                WeeklyCashflowBand.ClientReceipts,
                string.IsNullOrWhiteSpace(invoice.ContactName) ? "(no client)" : invoice.ContactName!.Trim(),
                InvoiceDetail(invoice),
                AgedReceivablesMaths.SignedAmountDue(invoice),
                AsDate(invoice.DueDate ?? invoice.Date),
                AsDate(invoice.ExpectedPaymentDate)));
        }

        return WeeklyCashflowMaths.Build(
            today,
            seeds,
            Plan.Current!.Items,
            Plan.Current.Placements,
            IsDirector && BankReady ? BankSnapshot!.TotalCash : null);
    }

    private static string BillDetail(XeroPayableBill bill)
    {
        var reference = bill.Number ?? bill.Reference ?? "no number";
        var flags = string.Join(" · ", new[]
        {
            bill.IsDraft ? "draft" : null,
            bill.IsCreditNote ? "credit note" : null
        }.Where(flag => flag is not null));
        return flags.Length > 0 ? $"{reference} · {flags}" : reference;
    }

    private static string InvoiceDetail(XeroReceivableInvoice invoice)
    {
        var reference = invoice.Number ?? invoice.Reference ?? "no number";
        var flags = string.Join(" · ", new[]
        {
            invoice.IsDraft ? "draft" : null,
            invoice.IsCreditNote ? "credit note" : null
        }.Where(flag => flag is not null));
        return flags.Length > 0 ? $"{reference} · {flags}" : reference;
    }

    // Re-kind before wrapping: a DateTime that arrives Kind=Local (offset-carrying JSON, a
    // future serializer change) would make DateTimeOffset(date, TimeSpan.Zero) throw off-UTC.
    private static DateTimeOffset? AsDate(DateTime? date) =>
        date is { } value ? new DateTimeOffset(DateTime.SpecifyKind(value.Date, DateTimeKind.Utc), TimeSpan.Zero) : null;

    private void ToggleBand(WeeklyCashflowBand band)
    {
        if (!collapsedBands.Remove(band)) collapsedBands.Add(band);
    }

    private void ToggleGroup(string supplierGroupId)
    {
        if (!expandedGroups.Remove(supplierGroupId)) expandedGroups.Add(supplierGroupId);
    }

    private HashSet<string> ExcludedKeys() =>
        Plan.Current is { } plan
            ? plan.Exclusions.Select(exclusion => exclusion.PlacementKey).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

    private string? ExcludedByFor(string placementKey) =>
        Plan.Current?.Exclusions.FirstOrDefault(exclusion => exclusion.PlacementKey == placementKey)?.ExcludedByEmail;


    /// <summary>The supplier groups as they land on this grid: each group with the band entries
    /// whose supplier (label) it holds. A group none of whose suppliers has a bill right now
    /// simply doesn't render. A supplier somehow in two groups counts once — the plan's first
    /// group wins.</summary>
    private IReadOnlyList<GroupSlice> GroupSlicesFor(WeeklyCashflowView view)
    {
        var plan = Plan.Current;
        if (plan is null || plan.SupplierGroups.Count == 0) return Array.Empty<GroupSlice>();

        var groupByContact = new Dictionary<string, WeeklyCashflowSupplierGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in plan.SupplierGroups)
            foreach (var contactName in group.ContactNames)
                groupByContact.TryAdd(contactName.Trim(), group);

        var members = new Dictionary<string, List<WeeklyCashflowEntry>>(StringComparer.Ordinal);
        foreach (var entry in WeeklyCashflowBands.EntriesFor(view, WeeklyCashflowBand.SupplierBills))
        {
            if (!groupByContact.TryGetValue(entry.Label, out var group)) continue;
            if (!members.TryGetValue(group.SupplierGroupId, out var list))
                members[group.SupplierGroupId] = list = new List<WeeklyCashflowEntry>();
            list.Add(entry);
        }

        return plan.SupplierGroups
            .Where(group => members.ContainsKey(group.SupplierGroupId))
            .Select(group => new GroupSlice(group, members[group.SupplierGroupId]))
            .ToList();
    }

}
