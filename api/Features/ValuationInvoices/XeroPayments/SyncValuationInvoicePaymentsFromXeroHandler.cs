using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroPayments;

/// <summary>
/// Applies the plan the preview shows, one invoice at a time so one refusal never holds the rest
/// (2026-09-11). A Link writes Xero's id and number (and XeroRaisedAt from Xero's date when blank)
/// straight onto the invoice with a RaisedInXero audit event; a RecordPayment goes through the
/// EXISTING RecordValuationInvoicePayment handler so the status move, the project's paid total and
/// the audit stay the one implementation — then PaidAt is overwritten with Xero's fully-paid date
/// (the handler stamps now) and the payment event's note says it came from Xero, in the same save.
/// Compiled into the nightly worker too (linked source), where SyncedBy is null.
/// </summary>
public sealed class SyncValuationInvoicePaymentsFromXeroHandler : ICommandHandler<SyncValuationInvoicePaymentsFromXero, ValuationInvoicePaymentSyncOutcome>
{
    public const string NightlyActor = "the nightly Xero payment sync";

    private readonly JpmsContext context;
    private readonly IXeroClient xero;
    private readonly ICommandHandler<RecordValuationInvoicePayment, ValuationInvoice> recordPayment;

    public SyncValuationInvoicePaymentsFromXeroHandler(
        JpmsContext context, IXeroClient xero, ICommandHandler<RecordValuationInvoicePayment, ValuationInvoice> recordPayment)
    {
        this.context = context;
        this.xero = xero;
        this.recordPayment = recordPayment;
    }

    public async Task<ValuationInvoicePaymentSyncOutcome> HandleAsync(SyncValuationInvoicePaymentsFromXero command, CancellationToken cancellationToken)
    {
        var plan = await new ValuationInvoicePaymentSyncPlanner(context, xero).PlanAsync(command.ProjectId, cancellationToken);
        if (plan.Blockers.Count > 0)
            throw new InvalidOperationException(string.Join(" ", plan.Blockers));

        var actor = string.IsNullOrWhiteSpace(command.SyncedBy) ? NightlyActor : command.SyncedBy.Trim();
        var results = new List<ValuationInvoicePaymentSyncResult>();
        int linked = 0, paid = 0, noChange = 0, failed = 0;

        foreach (var row in plan.Rows)
        {
            if (row.Action == ValuationInvoicePaymentSyncAction.None)
            {
                results.Add(new ValuationInvoicePaymentSyncResult(row, false, null));
                noChange++;
                continue;
            }
            try
            {
                if (row.Links)
                {
                    await LinkAsync(row, actor, cancellationToken);
                    linked++;
                }
                if (row.RecordsPayment)
                {
                    await RecordPaymentAsync(row, actor, cancellationToken);
                    paid++;
                }
                results.Add(new ValuationInvoicePaymentSyncResult(row, true, null));
            }
            catch (InvalidOperationException refusal)
            {
                results.Add(new ValuationInvoicePaymentSyncResult(row, false, refusal.Message));
                failed++;
            }
        }

        return new ValuationInvoicePaymentSyncOutcome(
            plan.Project.ProjectId, plan.Project.Name, plan.FetchedAtUtc, results, linked, paid, noChange, failed);
    }

    private async Task LinkAsync(ValuationInvoicePaymentSyncRow row, string actor, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.SingleAsync(candidate => candidate.ValuationInvoiceId == row.ValuationInvoiceId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(entity.XeroInvoiceId) || !string.IsNullOrWhiteSpace(entity.XeroInvoiceNumber))
            throw new InvalidOperationException($"{entity.Reference} was linked to {entity.XeroInvoiceNumber ?? entity.XeroInvoiceId} since the plan was read — sync again.");

        entity.XeroInvoiceId = row.XeroInvoiceId;
        entity.XeroInvoiceNumber = row.XeroInvoiceNumber;
        entity.XeroRaisedAt ??= XeroDateOf(row) ?? DateTimeOffset.UtcNow;
        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId, ValuationInvoiceEventType.RaisedInXero,
            $"Linked to Xero invoice {row.XeroInvoiceNumber ?? row.XeroInvoiceId} by {actor} — read back from Xero. {row.Note}".Trim(),
            amountAfter: entity.Amount);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordPaymentAsync(ValuationInvoicePaymentSyncRow row, string actor, CancellationToken cancellationToken)
    {
        // The one implementation of "paid": status, project paid total, audit.
        await recordPayment.HandleAsync(new RecordValuationInvoicePayment(row.ValuationInvoiceId, row.AmountToRecord ?? row.Amount), cancellationToken);

        // Xero's date, not the moment the sync ran — and the payment event says where it came from.
        // Same context, so the handler's tracked entities are the ones amended here.
        var entity = await context.ValuationInvoices.SingleAsync(candidate => candidate.ValuationInvoiceId == row.ValuationInvoiceId, cancellationToken);
        if (row.PaidOn is { } paidOn)
            entity.PaidAt = new DateTimeOffset(DateTime.SpecifyKind(paidOn.Date, DateTimeKind.Utc));
        var paymentEvent = context.ValuationInvoiceEvents.Local
            .Where(candidate => candidate.ValuationInvoiceId == row.ValuationInvoiceId
                && candidate.EventType == (int)ValuationInvoiceEventType.PaymentRecorded)
            .OrderByDescending(candidate => candidate.OccurredAt)
            .FirstOrDefault();
        if (paymentEvent is not null && string.IsNullOrWhiteSpace(paymentEvent.Note))
            paymentEvent.Note = $"Paid in Xero ({row.XeroInvoiceNumber ?? row.XeroInvoiceId}"
                + (row.PaidOn is { } on ? $" on {on:dd MMM yyyy}" : "")
                + $") — recorded by {actor}. {row.Note}".Trim();
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Xero's invoice date — when the invoice was raised there, which is what XeroRaisedAt means.</summary>
    private static DateTimeOffset? XeroDateOf(ValuationInvoicePaymentSyncRow row) =>
        row.XeroDate is { } date ? new DateTimeOffset(DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)) : null;
}
