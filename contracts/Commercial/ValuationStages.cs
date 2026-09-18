using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Where a valuation stands, read off the ONE record and its invoice (2026-09-18: the claim is
/// the valuation — tagged, reported on and invoiced from; there is no second state machine).
/// Draft → Locked (statement frozen) → Invoiced (VI raised) → Sent (claim recorded as sent) →
/// Approved → Certified (invoice issued — certified-to-date moves) → Paid → Confirmed (rolled
/// over). Rejected is the loop back: amend and resend. Derived, never stored: the persisted
/// statuses stay ValuationClaimStatus (Draft / Preapproved / Confirmed) and
/// ValuationInvoiceStatus — this is the one reading the page, the connector and a triager share.
/// </summary>
public enum ValuationStage
{
    Draft = 0,
    Locked = 1,
    Invoiced = 2,
    Sent = 3,
    Approved = 4,
    Rejected = 5,
    Certified = 6,
    Paid = 7,
    Confirmed = 8
}

public static class ValuationStages
{
    /// <summary>The stage of a claim given its live (non-cancelled, non-manual) invoice, if any.</summary>
    public static ValuationStage Of(ValuationClaim claim, ValuationInvoice? invoice)
    {
        if (claim.Status == ValuationClaimStatus.Confirmed) return ValuationStage.Confirmed;
        if (claim.Status == ValuationClaimStatus.Draft) return ValuationStage.Draft;
        if (invoice is null || invoice.Status == ValuationInvoiceStatus.Cancelled) return ValuationStage.Locked;
        return invoice.Status switch
        {
            ValuationInvoiceStatus.Raised => ValuationStage.Invoiced,
            ValuationInvoiceStatus.Submitted => ValuationStage.Sent,
            ValuationInvoiceStatus.Approved => ValuationStage.Approved,
            ValuationInvoiceStatus.Rejected => ValuationStage.Rejected,
            ValuationInvoiceStatus.Issued => ValuationStage.Certified,
            ValuationInvoiceStatus.Paid => ValuationStage.Paid,
            _ => ValuationStage.Locked
        };
    }

    /// <summary>The claim's live invoice among a project's register: the newest non-cancelled
    /// invoice drawn against it (manual entries name no claim, so they never match).</summary>
    public static ValuationInvoice? InvoiceFor(ValuationClaim claim, IEnumerable<ValuationInvoice> invoices) =>
        invoices
            .Where(invoice => invoice.ValuationClaimId == claim.ValuationClaimId
                              && invoice.Status != ValuationInvoiceStatus.Cancelled)
            .OrderByDescending(invoice => invoice.Number)
            .FirstOrDefault();

    public static string DisplayName(this ValuationStage stage) => stage switch
    {
        ValuationStage.Draft => "Draft",
        ValuationStage.Locked => "Locked",
        ValuationStage.Invoiced => "Invoiced",
        ValuationStage.Sent => "Sent to client",
        ValuationStage.Approved => "Approved",
        ValuationStage.Rejected => "Rejected",
        ValuationStage.Certified => "Certified",
        ValuationStage.Paid => "Paid",
        ValuationStage.Confirmed => "Confirmed",
        _ => stage.ToString()
    };
}
