using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

public sealed class CreateContractorsReportValidation
{
    public ValidationOutcome Check(CreateContractorsReport command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.PeriodEnd.DayOfWeek != DayOfWeek.Thursday) errors.Add("The reporting week ends on a Thursday.");
        if (command.Number is <= 0) errors.Add("The report number must be 1 or more.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UpdateContractorsReportValidation
{
    private const int ShortTextMax = 256;
    private const int ValuationNumberMax = 32;
    private const int NarrativeMax = 4000;
    private const int LiaisonMax = 2000;

    public ValidationOutcome Check(UpdateContractorsReport command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ContractorsReportId)) errors.Add("ContractorsReportId is required.");
        if (command.ValuationNumber.Length > ValuationNumberMax) errors.Add($"Valuation No. must be {ValuationNumberMax} characters or fewer.");
        if (command.ProgrammeReference.Length > ShortTextMax) errors.Add($"Programme reference must be {ShortTextMax} characters or fewer.");
        if (command.PreparedByName.Length > ShortTextMax) errors.Add($"Prepared by must be {ShortTextMax} characters or fewer.");
        if (command.IssuedTo.Length > ShortTextMax) errors.Add($"Issued to must be {ShortTextMax} characters or fewer.");
        if (command.Neighbours.Length > NarrativeMax) errors.Add($"Neighbours must be {NarrativeMax} characters or fewer.");
        if (command.HealthAndSafety.Length > NarrativeMax) errors.Add($"Health & Safety must be {NarrativeMax} characters or fewer.");
        if (command.BuildingControlLiaison.Length > LiaisonMax) errors.Add($"Building Control liaison must be {LiaisonMax} characters or fewer.");
        if (command.Attendance.Any(item => item.AttendanceDays is < 0)) errors.Add("Attendance days cannot be negative.");
        if (command.Attendance.Any(item => string.IsNullOrWhiteSpace(item.WorkOrderId))) errors.Add("Every attendance line names a work order.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
