using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CashCalls;

/// <summary>
/// Raises a monthly cash call against a project (optionally drawn from a valuation claim). Created in
/// the Requested state.
/// </summary>
public sealed record CreateCashCall(
    string ProjectId,
    DateTimeOffset PeriodMonth,
    decimal AmountRequested,
    string? ValuationClaimId = null) : ICommand<CashCall>;
