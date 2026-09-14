using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed partial class GetProjectSupplierAccountHandler
{
    private const string CreditNoteType = "ACCPAYCREDIT";

    // One account row per Xero bill. Net figures follow the ledger's sign convention — a credit
    // note subtracts — and the labour / materials split is by the account each line posts to:
    // the CIS labour account is labour, everything else is materials. The order shares are the
    // bill's link slices summed per order, which is exactly what the orders' side counts.
    private ProjectSupplierAccountInvoice BuildInvoice(
        SupplierBill bill,
        IReadOnlyList<XeroLineWorkOrderLinkEntity> links,
        IReadOnlyDictionary<string, string> referencesByOrder)
    {
        var head = bill.Head;
        var sign = string.Equals(head.Type, CreditNoteType, StringComparison.OrdinalIgnoreCase) ? -1m : 1m;
        var labour = bill.Lines.Where(IsLabour).Sum(line => bill.ProjectNetOf(line)) * sign;
        var materials = bill.Lines.Where(line => !IsLabour(line)).Sum(line => bill.ProjectNetOf(line)) * sign;
        var netElsewhere = bill.Lines.Sum(line => line.Net - bill.ProjectNetOf(line)) * sign;

        return new ProjectSupplierAccountInvoice(
            bill.XeroInvoiceId,
            string.IsNullOrWhiteSpace(head.InvoiceNumber) ? "(no invoice number)" : head.InvoiceNumber,
            head.Reference,
            head.Date,
            sign < 0m,
            labour,
            materials,
            netElsewhere,
            head.InvoiceStatus,
            head.InvoiceTotal,
            head.AmountDue,
            bill.Placement,
            OrderSharesOf(bill, links, referencesByOrder));
    }

    private bool IsLabour(XeroLedgerLineEntity line) =>
        string.Equals(line.AccountCode?.Trim(), xeroOptions.CisLabourAccountCode.Trim(), StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<ProjectSupplierAccountOrderShare> OrderSharesOf(
        SupplierBill bill,
        IReadOnlyList<XeroLineWorkOrderLinkEntity> links,
        IReadOnlyDictionary<string, string> referencesByOrder)
    {
        var lineIds = bill.Lines
            .Select(line => line.XeroLedgerLineId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return links
            .Where(link => lineIds.Contains(link.XeroLedgerLineId))
            .GroupBy(link => link.WorkOrderId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProjectSupplierAccountOrderShare(
                group.Key,
                referencesByOrder.TryGetValue(group.Key, out var reference) ? reference : group.Key,
                group.Sum(link => link.Amount)))
            .OrderBy(share => share.Reference, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
