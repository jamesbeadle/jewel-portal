using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Raised -> Submitted. The invoice already carries the snapshot frozen when it was raised — the
/// as-at-a-point-in-time statement (decision 2026-07-22) — so a first submission keeps it. Only an
/// invoice whose snapshot was flagged superseded by an amendment (or an older invoice raised
/// before raise-time capture existed) freezes a fresh one here, preserving the resubmit-after-
/// amendment behaviour: the client is always asked to approve the report behind the current ask.
/// </summary>
public sealed class SubmitValuationInvoiceHandler : ICommandHandler<SubmitValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public SubmitValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(SubmitValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.IsManual)
            throw new InvalidOperationException("Manual (historic) invoices record history — they don't go through approval.");
        if (entity.Status != (int)ValuationInvoiceStatus.Raised)
            throw new InvalidOperationException("Only a Raised valuation invoice can be submitted for approval.");

        // Live = not superseded; an amendment supersedes the attached snapshot, so its absence
        // here means the ask changed since the last freeze and a fresh statement is needed.
        var hasLiveSnapshot = await context.ValuationReportSnapshots
            .AnyAsync(snapshot => snapshot.ValuationInvoiceId == entity.ValuationInvoiceId
                                  && !snapshot.IsSuperseded, cancellationToken);
        if (!hasLiveSnapshot)
        {
            var snapshot = await ValuationReportSnapshotCapture.CaptureAsync(
                context, entity.ProjectId, $"{entity.Reference} submission", entity.ValuationInvoiceId, cancellationToken);
            entity.ValuationReportSnapshotId = snapshot.ValuationReportSnapshotId;
        }

        entity.Status = (int)ValuationInvoiceStatus.Submitted;
        entity.SubmittedAt = DateTimeOffset.UtcNow;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Submitted, "Submitted for approval.", amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
