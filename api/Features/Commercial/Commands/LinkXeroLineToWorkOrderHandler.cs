using System.Globalization;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class LinkXeroLineToWorkOrderHandler : ICommandHandler<LinkXeroLineToWorkOrder, Acknowledgement>
{
    private static readonly CultureInfo Gbp = CultureInfo.GetCultureInfo("en-GB");

    private readonly JpmsContext context;

    public LinkXeroLineToWorkOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(LinkXeroLineToWorkOrder command, CancellationToken cancellationToken)
    {
        var line = await context.XeroLedgerLines.FirstOrDefaultAsync(
            candidate => candidate.XeroLedgerLineId == command.XeroLedgerLineId, cancellationToken);
        if (line is null || !string.Equals(line.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This purchase line is not allocated to this project.");

        if (command.WorkOrderId is not null)
        {
            var order = await context.WorkOrders.FirstOrDefaultAsync(
                candidate => candidate.WorkOrderId == command.WorkOrderId && candidate.ProjectId == command.ProjectId,
                cancellationToken);
            if (order is null)
                throw new InvalidOperationException("That work order does not exist on this project.");
            if (order.Status == (int)WorkOrderStatus.Cancelled)
                throw new InvalidOperationException($"{order.Reference} is cancelled — invoices can't be linked to it.");

            // Linking classifies the whole ledger line, so a line split across cost
            // centres (its shares live in XeroCostSplits) can't be linked — the other
            // centres' shares would silently follow. Mirrors the UI restriction.
            var isSplit = line.CostCenterCode is null
                          || await context.XeroCostSplits.AnyAsync(
                              split => split.XeroLedgerLineId == line.XeroLedgerLineId, cancellationToken);
            if (isSplit)
                throw new InvalidOperationException(
                    "This line is split across cost centres, so it can't be linked to a work order. Re-cut it as a whole-line allocation first.");

            // Hard balance check: a link may never take the order past its value.
            // Credit notes subtract, so they (and zero-value lines) always fit.
            var signedNet = line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;
            if (signedNet > 0m)
            {
                var alreadyInvoiced = await context.XeroLedgerLines
                    .Where(other => other.LinkedWorkOrderId == command.WorkOrderId
                                    && other.XeroLedgerLineId != line.XeroLedgerLineId)
                    .Select(other => other.Type == "ACCPAYCREDIT" ? -other.Net : other.Net)
                    .SumAsync(cancellationToken);
                var remaining = order.Value - alreadyInvoiced;
                if (signedNet > remaining)
                    throw new InvalidOperationException(
                        $"This would over-invoice {order.Reference}: the line is {signedNet.ToString("C2", Gbp)} but only " +
                        $"{Math.Max(remaining, 0m).ToString("C2", Gbp)} of its {order.Value.ToString("C2", Gbp)} value is left to invoice.");
            }
        }

        line.LinkedWorkOrderId = command.WorkOrderId;
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(line.XeroLedgerLineId);
    }
}
