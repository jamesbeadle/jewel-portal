using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Raised -> Submitted. Freezes a full valuation-report snapshot — the exact report the client is
/// being asked to approve — and links it to the invoice. Resubmitting after an amendment captures
/// a fresh snapshot and flags the earlier one superseded (handled inside the capture).
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

        var snapshot = await ValuationReportSnapshotCapture.CaptureAsync(
            context, entity.ProjectId, $"{entity.Reference} submission", entity.ValuationInvoiceId, cancellationToken);

        entity.Status = (int)ValuationInvoiceStatus.Submitted;
        entity.SubmittedAt = DateTimeOffset.UtcNow;
        entity.ValuationReportSnapshotId = snapshot.ValuationReportSnapshotId;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Submitted, "Submitted for approval.", amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
