using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Amends a valuation invoice. Allowed while Raised or Rejected — any amendment flags the
/// invoice's snapshot superseded (a fresh one is frozen on the next submit/issue), and amending a
/// Rejected invoice additionally returns it to Raised and bumps the amendment count. Manual
/// invoices are editable at any status
/// (correcting history is the point) and may be amended to zero — voiding a mistaken entry's
/// value without losing the row: amount/paid/date changes adjust the project paid total and
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

        // Validation lets zero through so a manual entry can be voided; a workflow invoice for
        // nothing is a mistake, not an amendment.
        if (!entity.IsManual && command.Amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero — only manual entries can be zeroed.");

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

        // The statement behind the invoice is the locked claim, which an amendment of the
        // invoice's amount does not touch (2026-09-18): the audit row below carries the
        // before/after. Changing the FIGURES means cancel → reopen the claim → re-lock → re-raise.

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
