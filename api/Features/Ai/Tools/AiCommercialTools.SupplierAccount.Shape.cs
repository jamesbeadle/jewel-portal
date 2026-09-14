namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiCommercialTools
{
    // The account as the page shows it, camelCase, with the derived figures spelled out so the
    // model quotes them rather than re-adding rows.
    private static object ShapeSupplierAccount(ProjectSupplierAccount account) => new
    {
        ok = true,
        project = $"{account.ProjectReference} — {account.ProjectName}",
        projectId = account.ProjectId,
        supplier = account.SupplierName,
        subcontractorId = account.SubcontractorId,
        generatedAt = account.GeneratedAt,
        xeroSyncedAtUtc = account.LedgerSyncedAtUtc,
        position = new
        {
            ordered = account.Ordered,
            invoicedAndLinked = account.InvoicedAndLinked,
            paid = account.Paid,
            leftToInvoice = account.LeftToInvoice,
            received = account.Received,
            labourReceived = account.LabourReceived,
            materialsReceived = account.MaterialsReceived,
            settledByXero = account.ReceivedAndSettled,
            paymentsMade = account.PaymentsMade,
            cisDeducted = account.CisDeducted,
            notLinkedToAnOrder = account.Unmatched,
            overOrders = account.OverOrders,
            isOverInvoiced = account.IsOverInvoiced,
            invoicesAwaitingApproval = account.Invoices.Count(invoice => invoice.State == ProjectSupplierInvoiceState.AwaitingApproval)
        },
        orders = account.Orders.Select(order => new
        {
            workOrderId = order.WorkOrderId,
            reference = order.Reference,
            title = order.Title,
            status = order.Status.ToString(),
            awardedAt = order.AwardedAt,
            value = order.Value,
            invoicedAndLinked = order.InvoicedToDate,
            paid = order.IsPaymentKnown ? order.PaidToDate : (decimal?)null,
            paymentStatus = order.PaymentStatus.ToString(),
            leftToInvoice = order.RemainingToInvoice,
            lines = order.Lines.Select(line => new { title = line.Title, costCode = line.CostCode, lineTotal = line.LineTotal }).ToList(),
            invoicesLinked = order.Invoices.Select(invoice => new { xeroInvoiceId = invoice.XeroInvoiceId, invoiceNumber = invoice.InvoiceNumber, amount = invoice.Amount }).ToList()
        }).ToList(),
        invoices = account.Invoices.Select(invoice => new
        {
            xeroInvoiceId = invoice.XeroInvoiceId,
            invoiceNumber = invoice.InvoiceNumber,
            reference = invoice.Reference,
            date = invoice.Date,
            isCreditNote = invoice.IsCreditNote,
            labour = invoice.Labour,
            materials = invoice.Materials,
            net = invoice.Net,
            netOutsideProject = invoice.NetElsewhere,
            xeroStatus = invoice.XeroStatus,
            state = invoice.State.ToString(),
            stateLabel = invoice.State.Label(),
            placement = invoice.Placement.ToString(),
            placementNote = invoice.Placement.Label(),
            settledNet = invoice.SettledNet,
            paymentMade = invoice.HasPaymentDetail ? invoice.AmountPaid : (decimal?)null,
            cisDeducted = invoice.HasPaymentDetail ? invoice.CisDeduction : (decimal?)null,
            paidOn = invoice.PaidOn,
            matchedToOrders = invoice.Matched,
            notLinked = invoice.Unmatched,
            orders = invoice.Orders.Select(order => new { workOrderId = order.WorkOrderId, reference = order.Reference, amount = order.Amount }).ToList()
        }).ToList(),
        note = "All figures net of VAT and signed (credit notes negative). labour = the net on the CIS labour "
               + "account, materials = everything else, as the supplier raised the bill. paymentMade and "
               + "cisDeducted are Xero's figures for the WHOLE bill (null = Xero has recorded nothing yet: no "
               + "CIS is calculated until approval, nothing is paid until it is paid). An invoice with "
               + "state AwaitingApproval is received but nothing is owed on it until it is approved in Xero; "
               + "placement AwaitingAllocation / AssumedFromSupplier means nobody has coded it to the project "
               + "yet, so it is on nobody's WO Allocation queue. Linking an invoice to an order is done on the "
               + "WO Allocation tab or by approve_work_order_bill; a Variation 03 for work not on any order is "
               + "raised on the Variations tab. The PDF of this account is the Work orders tab's "
               + "Supplier account → Download PDF."
    };
}
