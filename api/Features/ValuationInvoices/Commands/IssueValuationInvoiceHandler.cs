using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Approved -> Issued (or Raised -> Issued for projects that skip the formal approval loop). From
/// here the amount counts toward "Certified to date". A report snapshot is normally frozen at
/// raise; issuing re-freezes only when no live one backs the invoice (amended since raise, or
/// pre-dating raise-time capture), so even two-click invoices keep the report behind them.
/// </summary>
public sealed class IssueValuationInvoiceHandler : ICommandHandler<IssueValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public IssueValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(IssueValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");

        switch ((ValuationInvoiceStatus)entity.Status)
        {
            case ValuationInvoiceStatus.Raised:
            case ValuationInvoiceStatus.Approved:
                break; // the two legal starting points
            case ValuationInvoiceStatus.Submitted:
                throw new InvalidOperationException("This valuation invoice is awaiting approval — approve or reject it first.");
            case ValuationInvoiceStatus.Rejected:
                throw new InvalidOperationException("This valuation invoice was rejected — amend and resubmit it first.");
            case ValuationInvoiceStatus.Cancelled:
                throw new InvalidOperationException("A cancelled valuation invoice cannot be issued.");
            case ValuationInvoiceStatus.Issued:
                throw new InvalidOperationException("This valuation invoice has already been issued.");
            case ValuationInvoiceStatus.Paid:
                throw new InvalidOperationException("A paid valuation invoice cannot be re-issued.");
        }

        // Make sure a LIVE report snapshot backs the invoice: raise-time capture normally
        // guarantees one, but an invoice amended since (snapshot flagged superseded) or raised
        // before raise-time capture existed needs a fresh freeze of the current ask.
        var hasLiveSnapshot = await context.ValuationReportSnapshots
            .AnyAsync(snapshot => snapshot.ValuationInvoiceId == entity.ValuationInvoiceId
                                  && !snapshot.IsSuperseded, cancellationToken);
        if (!hasLiveSnapshot)
        {
            var snapshot = await ValuationReportSnapshotCapture.CaptureAsync(
                context, entity.ProjectId, $"{entity.Reference} issue", entity.ValuationInvoiceId, cancellationToken);
            entity.ValuationReportSnapshotId = snapshot.ValuationReportSnapshotId;
        }

        entity.Status = (int)ValuationInvoiceStatus.Issued;
        entity.IssuedAt = DateTimeOffset.UtcNow;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Issued, "", amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);

        // Issuing raises "Certified to date" — re-freeze any Preapproved claim's totals so
        // the report summary reflects it (e.g. seeding historical claims under a claim
        // that was preapproved before the invoices existed).
        await PreapprovedClaimTotals.RefreshAsync(context, entity.ProjectId, cancellationToken);

        return entity.ToModel();
    }
}
