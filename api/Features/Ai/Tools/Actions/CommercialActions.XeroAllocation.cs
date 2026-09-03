using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The Xero Cost Allocation page's Allocate / Set / bucket / ignore / dispute buttons as ONE
/// connector action (2026-09-03). Until now the connector could read the whole ledger
/// (list_xero_ledger_lines) and link allocated lines to work orders, but the allocation itself —
/// the step that codes a purchase line to a project + cost centre and writes the tracking back to
/// Xero — was portal-only; the accountant ended up handing the director a list to key in by hand.
/// Same command, same handler, same Xero write-back as the page.
/// </summary>
internal sealed partial class CommercialActions
{
    // Replica of XeroLedgerRoles.AllowedToAllocate (the allocation page's gate, which
    // SetXeroAllocationAuthorisation enforces at execution).
    private static readonly RoleSet XeroAllocators =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    private static IEnumerable<AiAction> XeroAllocationActions() => new AiAction[]
    {
        new AiAction(
            Name: "set_xero_allocation",
            Area: "Commercial",
            Description: "Codes Xero purchase lines on the Xero Cost Allocation page — the Allocate / "
                + "Set / bucket / ignore / dispute buttons, applied to a batch of ledger line ids in one "
                + "call. action Allocate + projectId + costCenterCode codes every listed line to that "
                + "project and cost centre and takes it off the queue; WRITES TO XERO: once every line "
                + "of a DRAFT bill is allocated, its Sites + Cost Code tracking is written to the bill "
                + "and the bill is APPROVED (DRAFT → AUTHORISED); an already-approved bill whose line "
                + "moves project gets its Sites tracking rewritten (paid bills move portal-side only). "
                + "The write-back is best-effort — the allocation stands and Xero's answer is stamped on "
                + "the line (writeBackStatus). Allocate with splits (one line only; each share a "
                + "costCenterCode, net and optional projectId; nets must sum exactly to the line's net) "
                + "shares one line across centres or projects. action SetProject saves projectId (and "
                + "optionally costCenterCode) on queued or disputed lines WITHOUT allocating — the line "
                + "stays in the queue under that project and its Site tracking is written to Xero without "
                + "approving; projectId null unsets. AllocateToBucket + bucket parks non-project cost of "
                + "sales (Parking, Fuel, Tolls, Travel, Software subscriptions, ICA (Intercompany "
                + "Account), Other). Ignore (+ optional note) drops a line from the queue. Reset returns "
                + "lines to Unallocated, clearing their coding. Dispute (+ optional note as the opening "
                + "message) parks queued or allocated lines for the director and accountant to discuss; "
                + "AddDisputeMessage (note required) appends to that thread; ResolveDispute returns "
                + "disputed lines to the queue keeping the agreed coding. Any move off a line's project "
                + "clears its work-order links and package cost slices.",
            CommandType: typeof(SetXeroAllocation),
            ResultType: typeof(int),
            AuthorisationType: typeof(SetXeroAllocationAuthorisation),
            ValidationType: typeof(SetXeroAllocationValidation),
            VisibleTo: XeroAllocators,
            EmailStamps: new[] { nameof(SetXeroAllocation.AllocatedBy) },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "xeroLedgerLineIds come from list_xero_ledger_lines (status Unallocated for the queue; "
                + "each line carries suggestedProjectId / suggestedCostCenterCode from its Xero tracking, "
                + "never applied automatically). projectId from list_projects; costCenterCode from "
                + "list_cost_codes (the portal master 00001..00137, NOT Xero's option names). Batch lines "
                + "that share one project + cost centre into a single call; a different centre is a "
                + "separate call. In the confirm turn list every line (supplier, invoice number, net) "
                + "with the project + cost centre it is about to take, and say plainly which DRAFT bills "
                + "will be approved in Xero as a result — that is the irreversible-feeling part. The "
                + "result is the number of lines updated; re-read list_xero_ledger_lines to see the "
                + "write-back outcome per line. Lines can be re-allocated later (a change of mind is a "
                + "second Allocate), so a wrong centre is fixable; a wrong approval in Xero is not undone "
                + "from here."),
    };
}
