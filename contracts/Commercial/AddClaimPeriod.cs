using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record AddClaimPeriod(
    string ProjectId,
    int PeriodNumber,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate) : ICommand<ClaimPeriod>;
