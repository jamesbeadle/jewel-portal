using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices;

/// <summary>
/// Appends one entry to a valuation invoice's audit trail. Amendments are tracked on the same
/// invoice — no versioning — so this history is the record of every submission, rejection, and
/// amount change. Adds to the change tracker only; the calling handler saves in its own
/// transaction so the event and the state change land together.
/// </summary>
internal static class ValuationInvoiceAuditTrail
{
    public static void Append(
        JpmsContext context,
        string valuationInvoiceId,
        ValuationInvoiceEventType eventType,
        string note = "",
        decimal? amountBefore = null,
        decimal? amountAfter = null)
    {
        context.ValuationInvoiceEvents.Add(new ValuationInvoiceEventEntity
        {
            ValuationInvoiceEventId = ValuationInvoicesIdentifierFactory.NextValuationInvoiceEventId(),
            ValuationInvoiceId = valuationInvoiceId,
            EventType = (int)eventType,
            OccurredAt = DateTimeOffset.UtcNow,
            Note = note,
            AmountBefore = amountBefore,
            AmountAfter = amountAfter
        });
    }
}
