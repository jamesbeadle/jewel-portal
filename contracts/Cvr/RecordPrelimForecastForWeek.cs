using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cvr;

public sealed record RecordPrelimForecastForWeek(
    string ProjectId,
    string PrelimDescription,
    int WeekNumber,
    decimal TenderedAmount,
    decimal ActualAmount,
    decimal ForecastAmount) : ICommand<PrelimForecastEntry>;
