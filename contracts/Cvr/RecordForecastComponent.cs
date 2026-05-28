using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Cvr;

public sealed record RecordForecastComponent(
    string ProjectId,
    string PackageName,
    decimal CostIncurred,
    decimal CostCommitted,
    decimal QsAccrualAmount,
    decimal PrelimForecast,
    decimal CostToComplete) : ICommand<ForecastComponent>;
