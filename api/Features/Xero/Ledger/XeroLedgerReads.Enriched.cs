using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

internal static partial class XeroLedgerReads
{
    /// <summary>
    /// The line as the allocation page reads it — splits, suggestions, the dispute thread,
    /// labour recognition, Work Order bill recognition and the standing approval — for every
    /// entity in a read. The per-status read and the per-bill read share it (2026-09-14) so the
    /// queue a line sits in (<see cref="XeroLedgerQueues"/>, one rule) is computed the same way
    /// whichever way the line was reached. Suggestions and recognition are only computed while
    /// unallocated lines are in the read, so the settled tabs pay for none of it.
    /// </summary>
    public static async Task<IReadOnlyList<XeroLedgerLine>> ToModelsAsync(
        JpmsContext context, IReadOnlyList<XeroLedgerLineEntity> entities, CancellationToken cancellationToken)
    {
        var splitsByLine = await SplitsForAsync(context, entities, cancellationToken);
        var suggester = await SuggesterForAsync(context, entities, cancellationToken);
        var messagesByLine = await DisputeMessagesForAsync(context, entities, cancellationToken);
        // Labour recognition rides the same read as the suggestions so the queue, the Labour
        // section and the "re-check" refresh all see one rule.
        var labour = await LabourSupplierRecognition.ForAsync(context, entities, cancellationToken);
        // Work Order bill recognition rides it too (2026-09-08): a bill whose supplier has an open
        // order is decided per bill, labour recognition's answer and the bill's Sites hint in
        // hand, and the verdict lands on each of its lines.
        var workOrderBills = WorkOrderBillsFor(
            await WorkOrderBillRecognition.ForAsync(context, entities, cancellationToken), entities, labour, suggester);
        var approvalsByInvoice = await WorkOrderApprovalsForAsync(context, entities, cancellationToken);

        return entities.Select(entity => ToModel(
            entity,
            splitsByLine.TryGetValue(entity.XeroLedgerLineId, out var splits) ? splits : null,
            suggester,
            messagesByLine.TryGetValue(entity.XeroLedgerLineId, out var messages) ? messages : null,
            labour?.For(entity),
            workOrderBills.TryGetValue(entity.XeroLedgerLineId, out var workOrderBill) ? workOrderBill : null,
            approvalsByInvoice.TryGetValue(entity.XeroInvoiceId, out var approval) ? approval : null)).ToList();
    }
}
