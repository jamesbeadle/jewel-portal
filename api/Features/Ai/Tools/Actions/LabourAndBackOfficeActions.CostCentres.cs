using Jewel.JPMS.Api.Features.CostCenters.Commands;
using Jewel.JPMS.Api.Features.Xero.TrackingOptions;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Contracts.Xero;

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

        // ── Xero "Cost Code" tracking options: the master's shadow in Xero (2026-09-03) ──

        new AiAction(
            Name: "create_xero_cost_code_options",
            Area: "Cost centres",
            Description: "WRITES TO XERO: creates the \"Cost Code\" tracking options Xero is missing "
                + "for the portal's active cost codes — every one get_xero_cost_code_option_gaps "
                + "lists as missing, or only the codes named. Creates only: an option that already "
                + "exists (active or archived) is never touched and nothing is ever deleted or "
                + "archived. Stops at the first option Xero refuses and returns Xero's message "
                + "verbatim (the category's active-option cap is the expected refusal) with what "
                + "was created before it and what remains. Audited.",
            CommandType: typeof(CreateXeroCostCodeOptions),
            ResultType: typeof(XeroCostCodeOptionsCreateResult),
            AuthorisationType: typeof(CreateXeroCostCodeOptionsAuthorisation),
            ValidationType: typeof(CreateXeroCostCodeOptionsValidation),
            VisibleTo: CostCenterManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Call get_xero_cost_code_option_gaps FIRST and put its missing list (with the "
                + "active-option count) in the confirm turn — the user confirms against the "
                + "list. Leave codes out to create every missing option; pass codes to create a "
                + "subset. Archived options are reported, not recreated — restoring one is a "
                + "Xero-UI job. A refusal's text is Xero's own: show it as-is and stop."),

        new AiAction(
            Name: "rename_xero_cost_code_option",
            Area: "Cost centres",
            Description: "WRITES TO XERO: renames one existing \"Cost Code\" tracking option. Xero "
                + "applies the rename to HISTORY — every bill line ever tracked under the old "
                + "name reads under the new one in every report from then on — so this is never "
                + "a cosmetic change. The portal's cost-code master is NOT changed: when the old "
                + "name was a code's own name, the result warns that the code must be revised "
                + "(or its Xero mapping pointed at the new name) or the next bill recreates the "
                + "old option. Audited.",
            CommandType: typeof(RenameXeroCostCodeOption),
            ResultType: typeof(XeroCostCodeOptionRenameResult),
            AuthorisationType: typeof(RenameXeroCostCodeOptionAuthorisation),
            ValidationType: typeof(RenameXeroCostCodeOptionValidation),
            VisibleTo: CostCenterManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "In the confirm turn say, in so many words, that Xero rewrites history on a "
                + "rename — the user must confirm knowing that. currentName exactly as Xero "
                + "holds it (get_xero_cost_code_option_gaps shows the names). Follow up with "
                + "revise_cost_center or set_cost_code_xero_mapping when the result's warnings "
                + "say a portal code still codes under the old name."),
    };
}
