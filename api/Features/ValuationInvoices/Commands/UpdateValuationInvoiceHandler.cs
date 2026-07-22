using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Amends a valuation invoice. Allowed while Raised or Rejected — any amendment flags the
/// invoice's snapshot superseded (a fresh one is frozen on the next submit/issue), and amending a
/// Rejected invoice additionally returns it to Raised and bumps the amendment count. Manual
/// invoices are editable at any status
/// (correcting history is the point): amount/paid/date changes adjust the project paid total and
/// re-freeze any Preapproved claim, since "Certified to date" may have moved.
/// </summary>
public sealed class UpdateValuationInvoiceHandler : ICommandHandler<UpdateValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public UpdateValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(UpdateValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");

        var editable = entity.IsManual
            || entity.Status is (int)ValuationInvoiceStatus.Raised or (int)ValuationInvoiceStatus.Rejected;
        if (!editable)
            throw new InvalidOperationException(
                "This valuation invoice is locked — only Raised or Rejected invoices (or manual entries) can be amended.");

        var amountBefore = entity.Amount;
        var wasRejected = entity.Status == (int)ValuationInvoiceStatus.Rejected;
        var hadBeenSubmitted = entity.SubmittedAt is not null;

        entity.PeriodMonth = command.PeriodMonth;
        entity.Amount = command.Amount;

        if (entity.IsManual)
        {
            var paidBefore = entity.AmountPaid;
            var isPaid = command.PaidAt is not null || (command.AmountPaid ?? 0m) > 0m;
            var amountPaid = isPaid ? command.AmountPaid ?? command.Amount : 0m;

            entity.IssuedAt = command.IssuedAt ?? entity.IssuedAt ?? entity.PeriodMonth;
            entity.RaisedAt = entity.IssuedAt.Value;
            entity.AmountPaid = amountPaid;
            entity.PaidAt = isPaid ? command.PaidAt ?? entity.PaidAt ?? entity.IssuedAt : null;
            entity.Status = (int)(isPaid ? ValuationInvoiceStatus.Paid : ValuationInvoiceStatus.Issued);

            if (amountPaid != paidBefore)
            {
                var project = await context.Projects.FindAsync(new object[] { entity.ProjectId }, cancellationToken);
                if (project is not null) project.ValuationInvoicePaidTotal += amountPaid - paidBefore;
            }
        }
        else if (wasRejected)
        {
            // Amending a rejected invoice returns it to Raised, ready to resubmit. The rejection
            // stamp/reason stay on the record; the audit trail tells the story.
            entity.Status = (int)ValuationInvoiceStatus.Raised;
        }

        if (wasRejected || hadBeenSubmitted) entity.AmendmentCount += 1;

        // The attached snapshot froze the ask as it stood at raise (or last resubmission) — an
        // amendment changes the ask, so it no longer describes it. Flag it superseded; the next
        // submit/issue freezes a fresh one (the raise-time capture guarantees every non-manual
        // invoice has a snapshot to supersede).
        if (!entity.IsManual)
        {
            var snapshots = await context.ValuationReportSnapshots
                .Where(snapshot => snapshot.ValuationInvoiceId == entity.ValuationInvoiceId && !snapshot.IsSuperseded)
                .ToListAsync(cancellationToken);
            foreach (var snapshot in snapshots) snapshot.IsSuperseded = true;
        }

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Amended, command.Note ?? "",
            amountBefore: amountBefore, amountAfter: command.Amount);

        await context.SaveChangesAsync(cancellationToken);

        // Manual invoices are Issued/Paid — an amount change moves "Certified to date".
        if (entity.IsManual)
            await PreapprovedClaimTotals.RefreshAsync(context, entity.ProjectId, cancellationToken);

        return entity.ToModel();
    }
}
