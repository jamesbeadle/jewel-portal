using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Raises a monthly valuation invoice against a project (optionally drawn from a valuation claim). Created in
/// the Raised state.
/// </summary>
public sealed record CreateValuationInvoice(
    string ProjectId,
    DateTimeOffset PeriodMonth,
    decimal Amount,
    string? ValuationClaimId = null) : ICommand<ValuationInvoice>;
