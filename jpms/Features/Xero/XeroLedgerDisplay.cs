
namespace Jewel.JPMS.Features.Xero;

/// <summary>How a ledger line's money reads on screen — shared by the allocation page and its
/// dialogs so the two can never disagree about a credit note.</summary>
public static class XeroLedgerDisplay
{
    /// <summary>A credit note subtracts: ACCPAYCREDIT lines carry their net negated.</summary>
    public static decimal SignedNet(XeroLedgerLine line) =>
        line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;
}
