using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Gate classes for the settlement/Xero write cluster (2026-08-31), added so the connector's
// action gateway can compose the same checks the HTTP endpoints run inline. Each Allows reads
// the SAME RoleSet constant as its endpoint, so the two cannot drift (the parity-audit
// convention: the endpoint keeps its inline check, the gate class exists for the gateway).
// The validations mirror the endpoints' inline argument checks; where the portal's UI picker
// constrained a free-typed key (project, cost code), the master is checked here instead — the
// connector has no picker, and a mapping or variance against a key that exists nowhere would
// sit silent until the coding run skips on it.

// ---- SetSiteXeroMapping -----------------------------------------------------------------------

public sealed class SetSiteXeroMappingAuthorisation
{
    public bool Allows(SignedInUser user, SetSiteXeroMapping command) =>
        LabourRoleSets.ManageSettlement.IncludesAny(user.Roles);
}

public sealed class SetSiteXeroMappingValidation
{
    private readonly JpmsContext context;
    public SetSiteXeroMappingValidation(JpmsContext context) { this.context = context; }

    public async Task<ValidationOutcome> CheckAsync(SetSiteXeroMapping command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId))
            errors.Add("projectId is required — it comes from list_projects.");
        if (string.IsNullOrWhiteSpace(command.XeroTrackingOptionName))
            errors.Add("The Xero tracking option name is required — spelled exactly as Xero holds it "
                + "(the coding run matches sites by this option).");
        if (!string.IsNullOrWhiteSpace(command.ProjectId)
            && !await context.Projects.AsNoTracking().AnyAsync(project => project.ProjectId == command.ProjectId, cancellationToken))
            errors.Add($"No project with id \"{command.ProjectId}\" — the id comes from list_projects.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

// ---- SetCostCodeXeroMapping -------------------------------------------------------------------

public sealed class SetCostCodeXeroMappingAuthorisation
{
    public bool Allows(SignedInUser user, SetCostCodeXeroMapping command) =>
        LabourRoleSets.ManageSettlement.IncludesAny(user.Roles);
}

public sealed class SetCostCodeXeroMappingValidation
{
    private readonly JpmsContext context;
    public SetCostCodeXeroMappingValidation(JpmsContext context) { this.context = context; }

    public async Task<ValidationOutcome> CheckAsync(SetCostCodeXeroMapping command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.CostCode))
            errors.Add("A cost code is required — pick one from list_cost_codes.");
        else if (!await context.CostCenters.AsNoTracking()
                     .AnyAsync(centre => centre.Code == command.CostCode && centre.IsActive, cancellationToken))
            errors.Add($"\"{command.CostCode}\" is not an active cost code — call list_cost_codes and "
                + "use a Code exactly as it comes back.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

// ---- SetXeroLineTimesheetCover ----------------------------------------------------------------

public sealed class SetXeroLineTimesheetCoverAuthorisation
{
    public bool Allows(SignedInUser user, SetXeroLineTimesheetCover command) =>
        LabourRoleSets.ManageSettlement.IncludesAny(user.Roles);
}

public sealed class SetXeroLineTimesheetCoverValidation
{
    public ValidationOutcome Check(SetXeroLineTimesheetCover command)
    {
        var errors = new List<string>();
        // Mirror of the endpoint's inline check; the handler itself verifies the line exists.
        if (string.IsNullOrWhiteSpace(command.XeroLedgerLineId))
            errors.Add("xeroLedgerLineId is required — it comes from list_xero_ledger_lines.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

// ---- AddLabourSettlementVariance --------------------------------------------------------------

public sealed class AddLabourSettlementVarianceAuthorisation
{
    public bool Allows(SignedInUser user, AddLabourSettlementVariance command) =>
        LabourRoleSets.ManageSettlement.IncludesAny(user.Roles);
}

public sealed class AddLabourSettlementVarianceValidation
{
    private readonly JpmsContext context;
    public AddLabourSettlementVarianceValidation(JpmsContext context) { this.context = context; }

    public async Task<ValidationOutcome> CheckAsync(AddLabourSettlementVariance command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId))
            errors.Add("projectId is required — it comes from list_projects (the endpoint takes it "
                + "from the page route; the connector must say which project the variance lands on).");
        if (string.IsNullOrWhiteSpace(command.CostCode))
            errors.Add("A cost code is required.");
        if (command.Amount == 0m)
            errors.Add("A non-zero amount is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            errors.Add("A reason is required — settlement variances are never silent.");
        if (!string.IsNullOrWhiteSpace(command.ProjectId)
            && !await context.Projects.AsNoTracking().AnyAsync(project => project.ProjectId == command.ProjectId, cancellationToken))
            errors.Add($"No project with id \"{command.ProjectId}\" — the id comes from list_projects.");
        if (!string.IsNullOrWhiteSpace(command.CostCode)
            && !await context.CostCenters.AsNoTracking()
                .AnyAsync(centre => centre.Code == command.CostCode && centre.IsActive, cancellationToken))
            errors.Add($"\"{command.CostCode}\" is not an active cost code — call list_cost_codes and "
                + "use a Code exactly as it comes back.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
