using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.WeeklyCashflow;

/// <summary>One supplier group's slice of the plan: the group and the bills it pulls out of the
/// flat list into one combined row — on the grid and in the export alike, so the two always
/// agree about which bill belongs to which line.</summary>
public sealed record GroupSlice(WeeklyCashflowSupplierGroup Group, IReadOnlyList<WeeklyCashflowEntry> Entries)
{
    /// <summary>The supplier groups as they land on the grid: each group with the Supplier bills
    /// entries whose supplier (label) it holds, in the plan's own order. A group none of whose
    /// suppliers has a bill right now simply doesn't appear. A supplier somehow in two groups
    /// counts once — the plan's first group wins. Members read in grid order.</summary>
    public static IReadOnlyList<GroupSlice> For(WeeklyCashflowView view, IReadOnlyList<WeeklyCashflowSupplierGroup> groups)
    {
        if (groups.Count == 0) return Array.Empty<GroupSlice>();

        var groupByContact = new Dictionary<string, WeeklyCashflowSupplierGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
            foreach (var contactName in group.ContactNames)
                groupByContact.TryAdd(contactName.Trim(), group);

        var members = new Dictionary<string, List<WeeklyCashflowEntry>>(StringComparer.Ordinal);
        var supplierBills = view.Entries
            .Where(entry => entry.Band == WeeklyCashflowBand.SupplierBills)
            .InGridOrder();
        foreach (var entry in supplierBills)
        {
            if (!groupByContact.TryGetValue(entry.Label, out var group)) continue;
            if (!members.TryGetValue(group.SupplierGroupId, out var list))
                members[group.SupplierGroupId] = list = new List<WeeklyCashflowEntry>();
            list.Add(entry);
        }

        return groups
            .Where(group => members.ContainsKey(group.SupplierGroupId))
            .Select(group => new GroupSlice(group, members[group.SupplierGroupId]))
            .ToList();
    }
}
