using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Records the client's approval of a Submitted valuation invoice (Submitted -> Approved). The
/// invoice still doesn't count toward "Certified to date" until it is issued. An optional note
/// captures the certificate reference/date from the architect.
/// </summary>
public sealed record ApproveValuationInvoice(
    string ValuationInvoiceId,
    string? Note = null) : ICommand<ValuationInvoice>;
