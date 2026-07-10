using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Sends a Raised valuation invoice to the architect/client for approval (Raised -> Submitted).
/// Freezes a full valuation-report snapshot — every line with its % complete plus the summary
/// footer — and links it to the invoice, so "what did we ask for" is always answerable. Amount and
/// period are locked until the invoice is approved, rejected, or amended.
/// </summary>
public sealed record SubmitValuationInvoice(string ValuationInvoiceId) : ICommand<ValuationInvoice>;
