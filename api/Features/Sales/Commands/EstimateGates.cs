using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// One Authorisation + Validation pair per estimate command (SalesGates style): every write is the
// sales team's; reads are gated on SalesRoles.Readers at the endpoint.

public sealed class CreateEstimateAuthorisation
{
    public bool Allows(SignedInUser user, CreateEstimate command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class CreateEstimateValidation
{
    public ValidationOutcome Check(CreateEstimate command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.Scope)) errors.Add("Say what is to be priced (scope).");
        EstimateFieldLimits.Check(errors, command.Scope, command.ArchitectName, command.Notes, command.BudgetMentioned, command.Total);
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UpdateEstimateDetailsAuthorisation
{
    public bool Allows(SignedInUser user, UpdateEstimateDetails command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class UpdateEstimateDetailsValidation
{
    public ValidationOutcome Check(UpdateEstimateDetails command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.EstimateId)) errors.Add("EstimateId is required.");
        if (string.IsNullOrWhiteSpace(command.Scope)) errors.Add("Say what is to be priced (scope).");
        EstimateFieldLimits.Check(errors, command.Scope, command.ArchitectName, command.Notes, command.BudgetMentioned, command.Total);
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SetEstimateBreakdownAuthorisation
{
    public bool Allows(SignedInUser user, SetEstimateBreakdown command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class SetEstimateBreakdownValidation
{
    public ValidationOutcome Check(SetEstimateBreakdown command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.EstimateId)) errors.Add("EstimateId is required.");
        if (command.Sections is null) { errors.Add("Sections are required (an empty list clears the breakdown)."); return new ValidationOutcome(errors); }
        var sectionNumber = 0;
        foreach (var section in command.Sections)
        {
            sectionNumber++;
            if (string.IsNullOrWhiteSpace(section.Name)) errors.Add($"Section {sectionNumber} needs a name.");
            SalesFieldLimits.Check(errors, section.Name, 256, $"Section {sectionNumber} name");
            if (section.Lines is null) { errors.Add($"Section {sectionNumber} needs its lines (an empty list is allowed)."); continue; }
            var lineNumber = 0;
            foreach (var line in section.Lines)
            {
                lineNumber++;
                var where = $"Section {sectionNumber} line {lineNumber}";
                if (string.IsNullOrWhiteSpace(line.Description)) errors.Add($"{where} needs a description.");
                SalesFieldLimits.Check(errors, line.Description, 1024, $"{where} description");
                SalesFieldLimits.Check(errors, line.CostCode, 32, $"{where} cost code");
                SalesFieldLimits.Check(errors, line.Unit, 32, $"{where} unit");
                if (line.Quantity < 0) errors.Add($"{where}: quantity cannot be negative.");
                if (line.UnitPrice < 0) errors.Add($"{where}: unit price cannot be negative.");
            }
        }
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class MoveEstimateStatusAuthorisation
{
    public bool Allows(SignedInUser user, MoveEstimateStatus command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class MoveEstimateStatusValidation
{
    public ValidationOutcome Check(MoveEstimateStatus command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.EstimateId)) errors.Add("EstimateId is required.");
        if (!Enum.IsDefined(command.Status)) errors.Add("Status must be Received, Pricing, Submitted, Won or Lost.");
        SalesFieldLimits.Check(errors, command.Note, 4000, "Note");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

internal static class EstimateFieldLimits
{
    public static void Check(List<string> errors, string scope, string architectName, string notes, decimal? budgetMentioned, decimal? total)
    {
        SalesFieldLimits.Check(errors, scope, 4000, "Scope");
        SalesFieldLimits.Check(errors, architectName, 256, "Architect name");
        SalesFieldLimits.Check(errors, notes, 4000, "Notes");
        if (budgetMentioned is < 0) errors.Add("Budget mentioned cannot be negative.");
        if (total is < 0) errors.Add("Total cannot be negative.");
    }
}
