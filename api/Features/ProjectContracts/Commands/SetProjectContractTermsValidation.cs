using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// Messages are written as full sentences — they reach the user verbatim through the command
/// sender's error unwrapping.
/// </summary>
public sealed class SetProjectContractTermsValidation
{
    public ValidationOutcome Check(SetProjectContractTerms command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("A project is required.");
        if (!Enum.IsDefined(typeof(ContractForm), command.Form)) errors.Add("Choose a contract form.");

        if (command.ContractSum < 0) errors.Add("The contract sum cannot be negative.");
        if (command.LiquidatedDamagesPerWeek < 0) errors.Add("Liquidated damages cannot be negative.");

        if (command.RetentionPercent is < 0 or > 100)
            errors.Add("Retention must be between 0% and 100%.");
        if (command.RetentionPercentAfterCompletion is < 0 or > 100)
            errors.Add("Post-completion retention must be between 0% and 100%.");
        if (command.RetentionPercentAfterCompletion > command.RetentionPercent)
            errors.Add("Post-completion retention cannot exceed the pre-completion rate — release reduces it.");

        if (command.DefectsLiabilityPeriodMonths is < 0 or > 240)
            errors.Add("The defects liability period must be between 0 and 240 months.");

        if (command.ApplicationCutOffDayOfMonth is { } day && day is < 1 or > 28)
            errors.Add("The application cut-off day must be between 1 and 28, so it falls in every month.");

        if (command.PaymentNoticeDays is < 0 or > 90) errors.Add("The payment notice period must be between 0 and 90 days.");
        if (command.PayLessNoticeDays is < 0 or > 90) errors.Add("The pay-less notice period must be between 0 and 90 days.");
        if (command.FinalDateForPaymentDays is < 0 or > 180) errors.Add("The final date for payment must be between 0 and 180 days.");

        foreach (var (label, value) in new[]
                 {
                     ("Overheads and profit on direct works", command.OhpDirectWorksPercent),
                     ("Overheads and profit on subcontractor variations", command.OhpSubcontractorPercent),
                     ("General attendance", command.AttendanceOnClientDirectPercent),
                     ("Daywork labour", command.DayworkLabourPercent),
                     ("Daywork materials", command.DayworkMaterialsPercent),
                     ("Daywork plant", command.DayworkPlantPercent)
                 })
        {
            if (value is < 0 or > 100) errors.Add($"{label} must be between 0% and 100%.");
        }

        if (command.PossessionDate is { } possession && command.CompletionDate is { } completion
            && completion < possession)
        {
            errors.Add("The completion date cannot be before the date of possession.");
        }

        if (command.Form == ContractForm.Bespoke && string.IsNullOrWhiteSpace(command.BespokeDeviations))
        {
            // A bespoke form with no recorded deviations is worse than no record at all: anything
            // citing a clause would silently assume the standard form maps.
            errors.Add("Describe how the bespoke form differs from the standard — clause references depend on it.");
        }

        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
