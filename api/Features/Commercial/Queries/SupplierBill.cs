using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Subcontractors;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// One Xero bill on a supplier's account for a project, as <see cref="SupplierBillFinder"/> finds
/// it: every stored line of the bill, the net of each line that counts on this project (a whole
/// allocation's full net, a split's share, or a pending line's full net — positive, like the
/// entity, with the credit-note sign applied when the account is built), and how the bill
/// reached the account. A line absent from <see cref="ProjectNetByLineId"/> sits elsewhere —
/// another project, ignored or bucketed.
/// </summary>
internal sealed record SupplierBill(
    string XeroInvoiceId,
    IReadOnlyList<XeroLedgerLineEntity> Lines,
    IReadOnlyDictionary<string, decimal> ProjectNetByLineId,
    ProjectSupplierInvoicePlacement Placement)
{
    public XeroLedgerLineEntity Head => Lines[0];

    public decimal ProjectNetOf(XeroLedgerLineEntity line) =>
        ProjectNetByLineId.TryGetValue(line.XeroLedgerLineId, out var net) ? net : 0m;
}

/// <summary>
/// The names a supplier's bills can arrive under: the directory record's company name (matched
/// by the directory's own rule — DirectoryXeroMatcher) and the Xero contact name(s) it is linked
/// to (exact by construction). One rule for "is this bill theirs", shared with recognition.
/// </summary>
internal sealed class SupplierNames
{
    private readonly string companyName;
    private readonly HashSet<string> linkedContactNames;

    public SupplierNames(string companyName, IEnumerable<string> linkedContactNames)
    {
        this.companyName = companyName.Trim();
        this.linkedContactNames = linkedContactNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Contact names proven to be the supplier's by a bill already linked to their orders.</summary>
    public void Learn(IEnumerable<string?> contactNames)
    {
        foreach (var name in contactNames)
            if (!string.IsNullOrWhiteSpace(name)) linkedContactNames.Add(name.Trim());
    }

    public bool Matches(string? contactName)
    {
        if (string.IsNullOrWhiteSpace(contactName)) return false;
        if (linkedContactNames.Contains(contactName.Trim())) return true;
        return companyName.Length > 0 && DirectoryXeroMatcher.Matches(companyName, contactName);
    }
}

/// <summary>What the finder needs to know about the account it is finding bills for.</summary>
internal sealed record SupplierBillSearch(
    string ProjectId,
    SubcontractorEntity Supplier,
    IReadOnlySet<int> ProjectOrderNumbers,
    IReadOnlySet<string> LinkedLineIds,
    // The supplier holds live orders on this project and no other — an unplaced bill of theirs
    // can then be nobody else's.
    bool IsSupplierOnlyHere);
