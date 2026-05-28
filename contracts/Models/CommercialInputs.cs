namespace Jewel.JPMS.Models;

public sealed record Daywork(
    string DayworkId,
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
    decimal ChargeableAmount)
{
    public decimal TotalCost => LabourCost + PlantCost + MaterialsCost;
    public bool IsChargeableToClient => ChargeableAmount > 0;
}

public sealed record ContraCharge(
    string ContraChargeId,
    string ProjectId,
    string SubcontractorReference,
    DateTimeOffset RaisedOn,
    string Description,
    string Category,
    decimal Amount,
    string Status,
    decimal RecoveredAmount);

public sealed record SubcontractorRetention(
    string SubcontractorRetentionId,
    string ProjectId,
    string SubcontractorReference,
    decimal CertifiedAmount,
    decimal RetentionPercent,
    decimal FirstReleasedAmount,
    decimal FinalReleasedAmount)
{
    public decimal RetentionAmount => CertifiedAmount * RetentionPercent;
    public decimal BalanceHeld => RetentionAmount - FirstReleasedAmount - FinalReleasedAmount;
}
