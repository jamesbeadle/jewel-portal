using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// One Authorisation + Validation pair per command, so the connector's action registry resolves an
// unambiguous Allows/Check per command type (the same arrangement as the KPI register). Role sets
// live on SalesRoles — the actions reference the same sets.

public sealed class CaptureLeadAuthorisation
{
    public bool Allows(SignedInUser user, CaptureLead command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class CaptureLeadValidation
{
    public ValidationOutcome Check(CaptureLead command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ContactName) && string.IsNullOrWhiteSpace(command.CompanyName))
            errors.Add("A contact name or a company name is required.");
        if (string.IsNullOrWhiteSpace(command.OwnerEmail)) errors.Add("OwnerEmail is required.");
        if (command.Stage == LeadStage.Won) errors.Add("A lead cannot be captured as Won — capture it, then win it (WinLead) so the client and project are created.");
        SalesFieldLimits.Check(errors, command.ContactName, 256, "Contact name");
        SalesFieldLimits.Check(errors, command.ContactEmail, 256, "Contact email");
        SalesFieldLimits.Check(errors, command.ContactPhone, 64, "Contact phone");
        SalesFieldLimits.Check(errors, command.CompanyName, 256, "Company name");
        SalesFieldLimits.Check(errors, command.PropertyAddress, 512, "Property address");
        SalesFieldLimits.Check(errors, command.Postcode, 16, "Postcode");
        SalesFieldLimits.Check(errors, command.Summary, 512, "Summary");
        SalesFieldLimits.Check(errors, command.Notes, 4000, "Notes");
        if (command.EstimatedValue is < 0) errors.Add("Estimated value cannot be negative.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UpdateLeadAuthorisation
{
    public bool Allows(SignedInUser user, UpdateLead command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class UpdateLeadValidation
{
    public ValidationOutcome Check(UpdateLead command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.ContactName) && string.IsNullOrWhiteSpace(command.CompanyName))
            errors.Add("A contact name or a company name is required.");
        if (string.IsNullOrWhiteSpace(command.OwnerEmail)) errors.Add("OwnerEmail is required.");
        SalesFieldLimits.Check(errors, command.ContactName, 256, "Contact name");
        SalesFieldLimits.Check(errors, command.ContactEmail, 256, "Contact email");
        SalesFieldLimits.Check(errors, command.ContactPhone, 64, "Contact phone");
        SalesFieldLimits.Check(errors, command.CompanyName, 256, "Company name");
        SalesFieldLimits.Check(errors, command.PropertyAddress, 512, "Property address");
        SalesFieldLimits.Check(errors, command.Postcode, 16, "Postcode");
        SalesFieldLimits.Check(errors, command.Summary, 512, "Summary");
        SalesFieldLimits.Check(errors, command.Notes, 4000, "Notes");
        if (command.EstimatedValue is < 0) errors.Add("Estimated value cannot be negative.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class MoveLeadStageAuthorisation
{
    // Any open stage is the sales team's call; closing a lead (Lost) or parking it (Nurture) is a
    // decision — the directors'.
    public bool Allows(SignedInUser user, MoveLeadStage command) =>
        command.Stage.IsOpen()
            ? SalesRoles.SalesTeam.IncludesAny(user.Roles)
            : SalesRoles.Deciders.IncludesAny(user.Roles);
}

public sealed class MoveLeadStageValidation
{
    public ValidationOutcome Check(MoveLeadStage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (command.Stage == LeadStage.Won) errors.Add("Won is WinLead — it creates the client and the project shell.");
        if (command.Stage == LeadStage.Lost && string.IsNullOrWhiteSpace(command.LostReason)) errors.Add("Say why the lead was lost (lostReason).");
        SalesFieldLimits.Check(errors, command.Note, 4000, "Note");
        SalesFieldLimits.Check(errors, command.LostReason, 1024, "Lost reason");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class WinLeadAuthorisation
{
    public bool Allows(SignedInUser user, WinLead command) => SalesRoles.Deciders.IncludesAny(user.Roles);
}

public sealed class WinLeadValidation
{
    public ValidationOutcome Check(WinLead command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectReference)) errors.Add("Project reference is required (e.g. JBB-2026-014).");
        if (string.IsNullOrWhiteSpace(command.ProjectName)) errors.Add("Project name is required.");
        SalesFieldLimits.Check(errors, command.ProjectReference, 64, "Project reference");
        SalesFieldLimits.Check(errors, command.ProjectName, 256, "Project name");
        SalesFieldLimits.Check(errors, command.Note, 4000, "Note");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class LogLeadActivityAuthorisation
{
    public bool Allows(SignedInUser user, LogLeadActivity command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class LogLeadActivityValidation
{
    public ValidationOutcome Check(LogLeadActivity command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.Summary)) errors.Add("Say what happened (summary).");
        if (command.Kind == LeadActivityKind.StageChange) errors.Add("Stage changes are written by MoveLeadStage / WinLead, not logged by hand.");
        SalesFieldLimits.Check(errors, command.Summary, 4000, "Summary");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class CreateSalesStrategyAuthorisation
{
    public bool Allows(SignedInUser user, CreateSalesStrategy command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class CreateSalesStrategyValidation
{
    public ValidationOutcome Check(CreateSalesStrategy command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(command.OwnerEmail)) errors.Add("OwnerEmail is required.");
        SalesFieldLimits.CheckStrategy(errors, command.Name, command.TargetArea, command.Hypothesis, command.Evidence, command.Proposition);
        SalesFieldLimits.Check(errors, command.Brief, 4000, "Brief");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UpdateSalesStrategyAuthorisation
{
    public bool Allows(SignedInUser user, UpdateSalesStrategy command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class UpdateSalesStrategyValidation
{
    public ValidationOutcome Check(UpdateSalesStrategy command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.StrategyId)) errors.Add("StrategyId is required.");
        if (string.IsNullOrWhiteSpace(command.Name)) errors.Add("Name is required.");
        if (string.IsNullOrWhiteSpace(command.OwnerEmail)) errors.Add("OwnerEmail is required.");
        SalesFieldLimits.CheckStrategy(errors, command.Name, command.TargetArea, command.Hypothesis, command.Evidence, command.Proposition);
        SalesFieldLimits.Check(errors, command.Brief, 4000, "Brief");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class RunStrategyResearchAuthorisation
{
    public bool Allows(SignedInUser user, RunStrategyResearch command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class RunStrategyResearchValidation
{
    public ValidationOutcome Check(RunStrategyResearch command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.StrategyId)) errors.Add("StrategyId is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SetSalesStrategyStatusAuthorisation
{
    public bool Allows(SignedInUser user, SetSalesStrategyStatus command) => SalesRoles.Deciders.IncludesAny(user.Roles);
}

public sealed class SetSalesStrategyStatusValidation
{
    public ValidationOutcome Check(SetSalesStrategyStatus command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.StrategyId)) errors.Add("StrategyId is required.");
        if (!Enum.IsDefined(command.Status)) errors.Add("Status must be Draft, Active, Paused or Retired.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class GenerateStrategyApproachPlanAuthorisation
{
    public bool Allows(SignedInUser user, GenerateStrategyApproachPlan command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class GenerateStrategyApproachPlanValidation
{
    public ValidationOutcome Check(GenerateStrategyApproachPlan command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.StrategyId)) errors.Add("StrategyId is required.");
        SalesFieldLimits.Check(errors, command.Guidance, 2000, "Guidance");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

internal static class SalesFieldLimits
{
    public static void Check(List<string> errors, string? value, int max, string label)
    {
        if (value is { Length: var length } && length > max) errors.Add($"{label} is limited to {max} characters.");
    }

    public static void CheckStrategy(List<string> errors, string name, string targetArea, string hypothesis, string evidence, string proposition)
    {
        Check(errors, name, 256, "Name");
        Check(errors, targetArea, 512, "Target area");
        Check(errors, hypothesis, 4000, "Hypothesis");
        Check(errors, evidence, 4000, "Evidence");
        Check(errors, proposition, 1024, "Proposition");
    }
}
