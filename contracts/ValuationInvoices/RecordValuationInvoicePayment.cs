using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Records the client's payment against a valuation invoice (-> Paid) and increases the project-level
/// valuation-invoice total by the amount received.
/// </summary>
public sealed record RecordValuationInvoicePayment(
    string ValuationInvoiceId,
    decimal AmountPaid) : ICommand<ValuationInvoice>;
