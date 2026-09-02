using Jewel.JPMS.Api.Features.CostCenters.Commands;
using Jewel.JPMS.Contracts.CostCenters;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> CostCentreActions() => new AiAction[]
    {
        new AiAction(
            Name: "add_cost_center",
            Area: "Cost centres",
            Description: "Adds a cost code to the GLOBAL cost-center master — it appears at once in "
                + "the cost-code dropdowns and the Financials views that every project's money is "
                + "coded against. This is a commercial control shared by all projects, not a "
                + "per-project setting.",
            CommandType: typeof(AddCostCenter),
            ResultType: typeof(CostCenter),
            AuthorisationType: typeof(AddCostCenterAuthorisation),
            ValidationType: typeof(AddCostCenterValidation),
            VisibleTo: CostCenterManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Pass sortOrder 0 to append after the current last code. Duplicate codes are "
                + "refused by the handler."),

        new AiAction(
            Name: "revise_cost_center",
            Area: "Cost centres",
            Description: "Revises a cost code in the global cost-center master — code, name, order "
                + "and active flag — changing how money is coded on every project from now on. "
                + "Setting isActive false retires the code: it drops out of dropdowns and the "
                + "Financials view without deleting it, so historical allocations keep resolving.",
            CommandType: typeof(ReviseCostCenter),
            ResultType: typeof(CostCenter),
            AuthorisationType: typeof(ReviseCostCenterAuthorisation),
            ValidationType: typeof(ReviseCostCenterValidation),
            VisibleTo: CostCenterManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "costCenterId identifies the existing code (over HTTP it is the route value). "
                + "Confirm with the user before retiring a code — it disappears from every "
                + "project's dropdowns at once."),
    };
}
