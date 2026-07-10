using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Records the client's rejection of a Submitted valuation invoice (Submitted -> Rejected). The
/// reason is required — it drives the amendment. A Rejected invoice is unlocked: amend it (back to
/// Raised, fresh snapshot on resubmit) or cancel it.
/// </summary>
public sealed record RejectValuationInvoice(
    string ValuationInvoiceId,
    string Reason) : ICommand<ValuationInvoice>;
