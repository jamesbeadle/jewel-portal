using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record ReviseValuation(
    string ValuationId,
    decimal GrossValue,
    decimal RetentionPercent) : ICommand<Valuation>;
