using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Deletes a valuation invoice — one raised in error, or being replaced after an
/// adjustment. Removing an Issued/Paid invoice reduces "Certified to date" (and any
/// Preapproved claim's frozen totals are re-frozen to match); deleting a Paid one
/// also rolls its receipt back out of the project's paid total.
/// </summary>
public sealed record DeleteValuationInvoice(string ValuationInvoiceId) : ICommand<Acknowledgement>;
