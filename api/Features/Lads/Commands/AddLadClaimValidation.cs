using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Lads;

namespace Jewel.JPMS.Api.Features.Lads.Commands;

public sealed class AddLadClaimValidation
{
    public ValidationOutcome Check(AddLadClaim command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("Title is required.");
        if (command.DaysClaimed < 0) errors.Add("Days claimed cannot be negative.");
        if (command.RatePerWeek < 0) errors.Add("Rate per week cannot be negative.");
        if (command.Amount < 0) errors.Add("Amount cannot be negative.");
        if (command.PeriodFrom is not null && command.PeriodTo is not null && command.PeriodTo < command.PeriodFrom)
            errors.Add("The period end cannot be before its start.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
