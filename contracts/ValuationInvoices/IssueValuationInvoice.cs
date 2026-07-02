using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>Marks a valuation invoice's client invoice as prepared (Raised -> Issued).</summary>
public sealed record IssueValuationInvoice(string ValuationInvoiceId) : ICommand<ValuationInvoice>;
