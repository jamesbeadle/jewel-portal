using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Marks a valuation invoice's client invoice as sent (Approved -> Issued, or Raised -> Issued for
/// projects that skip the formal approval loop). From this point the amount counts toward
/// "Certified to date". The skip path freezes a report snapshot if none is linked yet, so even
/// two-click invoices keep a record of the report behind them.
/// </summary>
public sealed record IssueValuationInvoice(string ValuationInvoiceId) : ICommand<ValuationInvoice>;
