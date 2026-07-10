using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>Audit trail for one valuation invoice, oldest first.</summary>
public sealed record ListValuationInvoiceEvents(string ValuationInvoiceId) : IQuery<IReadOnlyList<ValuationInvoiceEvent>>;
