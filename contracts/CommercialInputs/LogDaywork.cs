using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CommercialInputs;

public sealed record LogDaywork(
    string ProjectId,
    DateTimeOffset WorkedOn,
    string SubcontractorReference,
    string Description,
    string InstructedBy,
    decimal Hours,
    decimal HourlyRate,
    decimal LabourCost,
    decimal PlantCost,
    decimal MaterialsCost,
    decimal UpliftPercent,
    decimal ChargeableAmount) : ICommand<Daywork>;
