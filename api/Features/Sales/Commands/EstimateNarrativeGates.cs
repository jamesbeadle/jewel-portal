using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

/// <summary>The narrative write's gates: the sales team's, and the three texts within the lengths
/// the record holds.</summary>
public sealed class SetEstimateNarrativeAuthorisation
{
    public bool Allows(SignedInUser user, SetEstimateNarrative command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class SetEstimateNarrativeValidation
{
    public ValidationOutcome Check(SetEstimateNarrative command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.EstimateId)) errors.Add("EstimateId is required.");
        SalesFieldLimits.Check(errors, command.ExecutiveSummary, 20000, "Executive summary");
        SalesFieldLimits.Check(errors, command.BuildTime, 1024, "Build time");
        SalesFieldLimits.Check(errors, command.Exclusions, 4000, "Exclusions");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
