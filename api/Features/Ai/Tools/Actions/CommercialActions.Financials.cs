using Jewel.JPMS.Api.Features.Cashflow.Commands;
using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Api.Features.CommercialInputs.Commands;
using Jewel.JPMS.Api.Features.Cvr.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cashflow;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class CommercialActions
{
    private static IEnumerable<AiAction> FinancialsActions() => new AiAction[]
    {
        // ── Commercial: Financials tab — budgets, cost centres, groups, packages ─────────

        new AiAction(
            Name: "set_cost_code_budget",
            Area: "Commercial",
            Description: "Sets a cost code's budget on a project — the allocated amount and spent "
                + "amount that the Financials tab reads. Upserts the budget row for that code. "
                + "The figures sent are ABSOLUTE, not deltas: read the current row with "
                + "get_cost_code_budgets first and compute the new figure from it. Omit "
                + "spentAmount to leave the recorded spend untouched. Every change writes a "
                + "before → after row to the audit trail.",
            CommandType: typeof(SetCostCodeBudget),
            ResultType: typeof(CostCodeBudget),
            AuthorisationType: typeof(SetCostCodeBudgetAuthorisation),
            ValidationType: typeof(SetCostCodeBudgetValidation),
            VisibleTo: ValuationDrafters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "projectId comes from list_projects; costCode from list_cost_codes. In the "
                + "confirm turn show the current figures (get_cost_code_budgets) next to the "
                + "proposed ones so the user sees exactly what moves — and when the change is "
                + "raising an allocation an overspend already burst, say so plainly; re-coding "
                + "the cost, a work order, or an MD/FD over-budget approval may be the honest "
                + "route instead."),

        new AiAction(
            Name: "set_cost_centre_cost_completion",
            Area: "Commercial",
            Description: "Sets the cost-side completion percentage for one cost centre on a project — "
                + "the commercial team's assessment of how far through the cost of the work they are, "
                + "shown on the Financials tab. Distinct from sales-side completion, which comes from "
                + "the latest claim. Upserts.",
            CommandType: typeof(SetCostCentreCostCompletion),
            ResultType: typeof(CostCentreCostProgress),
            AuthorisationType: typeof(SetCostCentreCostCompletionAuthorisation),
            ValidationType: typeof(SetCostCentreCostCompletionValidation),
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; costCode from list_cost_codes."),

        new AiAction(
            Name: "set_cost_centre_finalisation",
            Area: "Commercial",
            Description: "Locks a cost centre down on the Financials tab (or unlocks it). A finalised "
                + "centre expects no further spend: its remaining drawdown reads as realised profit or "
                + "loss instead of funds still available — changing how the project's money position is "
                + "read by everyone.",
            CommandType: typeof(SetCostCentreFinalisation),
            ResultType: typeof(CostCentreCostProgress),
            AuthorisationType: typeof(SetCostCentreFinalisationAuthorisation),
            ValidationType: typeof(SetCostCentreFinalisationValidation),
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before locking or unlocking a centre — it changes how "
                + "remaining funds are reported. costCode comes from list_cost_codes."),

        new AiAction(
            Name: "create_cost_centre_group",
            Area: "Commercial",
            Description: "Creates a named roll-up of two or more cost centres on the Financials tab so "
                + "related centres read as one line. Presentation only — no underlying money moves. "
                + "Rejected when a centre already sits in another group, unless that group is listed in "
                + "replaceGroupIds to be dissolved and absorbed in the same save.",
            CommandType: typeof(CreateCostCentreGroup),
            ResultType: typeof(CostCentreGroup),
            AuthorisationType: typeof(CreateCostCentreGroupAuthorisation),
            ValidationType: typeof(CreateCostCentreGroupValidation),
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; costCodes from list_cost_codes."),

        new AiAction(
            Name: "remove_cost_centre_group",
            Area: "Commercial",
            Description: "Dissolves a cost centre roll-up; its centres return to individual rows on the "
                + "Financials tab. Presentation only — nothing else is deleted and no money moves.",
            CommandType: typeof(RemoveCostCentreGroup),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(CreateCostCentreGroupAuthorisation),
            ValidationType: null,
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "costCentreGroupId comes from the project's cost centre groups list."),

        new AiAction(
            Name: "save_reconciliation_package",
            Area: "Commercial",
            Description: "Creates or wholly replaces a reconciliation package's definition — the tie "
                + "between work orders (cost side) and valuation sales lines or £ slices (sales side) "
                + "that the Financials tab reports profit per package from. Presentation only; nothing "
                + "writes to Xero. Locked packages cannot be edited.",
            CommandType: typeof(SaveReconciliationPackage),
            ResultType: typeof(ReconciliationPackage),
            AuthorisationType: typeof(ReconciliationPackageAuthorisation),
            ValidationType: typeof(SaveReconciliationPackageValidation),
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Null reconciliationPackageId creates; an existing id replaces that package's whole "
                + "definition — read the current definition first and carry forward what should not "
                + "change. Work order ids come from list_work_orders."),

        new AiAction(
            Name: "remove_reconciliation_package",
            Area: "Commercial",
            Description: "Dissolves a reconciliation package (it must be unlocked). Nothing underneath "
                + "is deleted and no money moves — the package is presentation only.",
            CommandType: typeof(RemoveReconciliationPackage),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ReconciliationPackageAuthorisation),
            ValidationType: null,
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which package, by name, before calling."),

        new AiAction(
            Name: "set_reconciliation_package_lock",
            Area: "Commercial",
            Description: "Locks a reconciliation package — freezing its figures and realising profit or "
                + "loss against actual invoiced cost rather than committed orders — or unlocks it, "
                + "clearing the snapshot so the figures go live again.",
            CommandType: typeof(SetReconciliationPackageLock),
            ResultType: typeof(ReconciliationPackage),
            AuthorisationType: typeof(ReconciliationPackageAuthorisation),
            ValidationType: null,
            VisibleTo: FinancialsTabManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before locking or unlocking — locking banks profit/loss "
                + "figures on the Financials tab."),

        new AiAction(
            Name: "set_valuation_line_cost_centre",
            Area: "Commercial",
            Description: "Recodes which cost centre a valuation line's value sits against — a financial "
                + "correction that moves the line's value between cost centres without changing the "
                + "agreed amount. Exists so finance can correct allocation on variation lines frozen at "
                + "VO approval. The change is audited.",
            CommandType: typeof(SetValuationLineCostCentre),
            ResultType: typeof(ValuationLineItem),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(SetValuationLineCostCentreValidation),
            VisibleTo: CostCentreRecoders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "valuationLineItemId comes from the valuation report's lines; costCode from "
                + "list_cost_codes."),

        new AiAction(
            Name: "set_client_cost_references",
            Area: "Commercial",
            Description: "Replaces the project's WHOLE cost centre to client schedule-of-works "
                + "reference map in one save: entries with a reference are kept, blank references are "
                + "removed, and any cost centre not listed is removed too. Report setup — no amounts "
                + "change.",
            CommandType: typeof(SetClientCostReferences),
            ResultType: typeof(IReadOnlyList<ClientCostReference>),
            AuthorisationType: typeof(ValuationReportAuthorisation),
            ValidationType: typeof(SetClientCostReferencesValidation),
            VisibleTo: ClaimLifecycleManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This is a full replace — read the current map first and include every entry that "
                + "should survive, or it will be removed."),

        new AiAction(
            Name: "set_xero_line_work_order_links",
            Area: "Commercial",
            Description: "Replaces the set of work-order links on an allocated Xero purchase line — "
                + "deciding which work orders that invoice money counts against (invoiced-to-date). One "
                + "full-net slice is the everyday whole-line link; several slices split a bill across "
                + "orders; an empty list clears all links. Slices may total less than the line (the "
                + "remainder counts as non-work-order cost of sales) but never more.",
            CommandType: typeof(SetXeroLineWorkOrderLinks),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(SetXeroLineWorkOrderLinksAuthorisation),
            ValidationType: typeof(SetXeroLineWorkOrderLinksValidation),
            VisibleTo: XeroWorkOrderLinkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "This replaces ALL links on the line — include every slice that should remain. Work "
                + "order ids come from list_work_orders; no slice may take an order past its value."),

        // ── Commercial: timesheets ───────────────────────────────────────────────────────
        // Deliberately NO actions here any more (actions removed 2026-08-28; the slices
        // themselves were deleted the same day). The legacy Commercial SubmitTimesheet/
        // ApproveTimesheet slices predated the worker register: their rows carried a free-typed
        // personEmail and no WorkerId, so the Labour approval refused them ("No worker record"),
        // and the schema taught models to demand worker emails the portal does not need — the
        // accountant's first connector session was asked to invent emails for the whole crew.
        // The connector's labour entry is submit_worker_week (LabourAndBackOfficeActions);
        // approval stays in the portal's Labour tab, where the rate snapshot and the budget
        // hard-block live (the legacy ApproveTimesheet set IsApproved without either).

    };
}
