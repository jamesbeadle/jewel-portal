using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;


public static partial class ModalCatalog
{
    /// <summary>
    /// The "Set % complete" dialog on a project's Valuation Report tab (2026-08-25): "review and
    /// correct the % complete against the valuation" means the assistant reads the report
    /// (get_valuation_context — every line with its id, current % and previous %), reads the
    /// evidence, and puts the corrected percentages into this dialog for the user to check and
    /// press Save. It records entries on the SELECTED claim, which must be a Draft — the page
    /// refuses otherwise, and the dialog's state names the claim.
    /// </summary>
    public static readonly ModalDescriptor ClaimProgress = new(
        "claim_progress",
        "Set % complete",
        "It sets the cumulative % complete on lines of the Valuation Report's selected Draft claim "
        + "— the same act as typing into the report's % column, batched. Read "
        + "get_valuation_context first: it gives every line's valuationLineItemId, its current % "
        + "on the claim and the previous claim's %, and says which claim is selected and whether "
        + "it is Draft. Send only lines whose % should change, as CUMULATIVE percentages (what is "
        + "complete to date, not this period's increment); 0–100 on contract lines, wider on "
        + "variation lines. The entries sent replace the dialog's pending list. The user reviews "
        + "them and presses Save themselves; nothing is recorded until they do.",
        "/projects/{project}/valuation",
        // Exactly the API's gate for recording claim entries (ValuationReportAuthorisation
        // .RolesThatMayRecordClaimEntries): Director, FD, PM, QS — plus administrators.
        new[]
        {
            Role.Admin,
            Role.ManagingDirector,
            Role.FinanceDirector,
            Role.ProjectManager,
            Role.QuantitySurveyor
        },
        new ModalField[]
        {
            new("entries", "array",
                "The lines to change, each with its new cumulative % complete — this replaces the "
                + "dialog's pending list. Only lines whose % should change; every entry needs the "
                + "line's valuationLineItemId from get_valuation_context.",
                Required: true,
                ItemFields: new ModalField[]
                {
                    new("valuationLineItemId", "string",
                        "The report line, exactly as get_valuation_context returned it.", Required: true),
                    new("percentComplete", "number",
                        "The cumulative % complete to date as a plain number — 100 for finished. "
                        + "Not the period's increment.", Required: true)
                })
        });

}
