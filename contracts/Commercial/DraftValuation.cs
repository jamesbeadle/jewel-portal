using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record DraftValuation(
    string ProjectId,
    string ClaimPeriodId,
    decimal GrossValue,
    decimal RetentionPercent) : ICommand<Valuation>;
