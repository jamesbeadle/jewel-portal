using Jewel.JPMS.Api.Features.Boq.Commands;
using Jewel.JPMS.Api.Features.Lads;
using Jewel.JPMS.Api.Features.Lads.Commands;
using Jewel.JPMS.Api.Features.Retention.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class VariationsAndValuationsActions
{
    private static IEnumerable<AiAction> BoqActions() => new AiAction[]
    {
        new AiAction(
            Name: "add_boq_line",
            Area: "BoQ",
            Description: "Adds a priced line to a project's Bill of Quantities — description, unit, "
                + "quantity, rate, cost code and discipline. The BoQ is the tender-side pricing "
                + "record the sign-off freezes against.",
            CommandType: typeof(AddBoqLine),
            ResultType: typeof(BoqLineItem),
            AuthorisationType: typeof(AddBoqLineAuthorisation),
            ValidationType: typeof(AddBoqLineValidation),
            VisibleTo: BoqEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects; cost codes from list_cost_codes."),

        new AiAction(
            Name: "update_boq_line",
            Area: "BoQ",
            Description: "Updates an existing BoQ line's details — description, unit, quantity, "
                + "rate, cost code and discipline. The whole line is re-stated each call.",
            CommandType: typeof(UpdateBoqLine),
            ResultType: typeof(BoqLineItem),
            AuthorisationType: typeof(UpdateBoqLineAuthorisation),
            ValidationType: typeof(UpdateBoqLineValidation),
            VisibleTo: BoqEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "boqLineItemId comes from the project's BoQ listing."),

        new AiAction(
            Name: "remove_boq_line",
            Area: "BoQ",
            Description: "Removes a line from a project's Bill of Quantities permanently. There is "
                + "no undo.",
            CommandType: typeof(RemoveBoqLine),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveBoqLineAuthorisation),
            ValidationType: typeof(RemoveBoqLineValidation),
            VisibleTo: BoqEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which line, by description, before calling."),

        new AiAction(
            Name: "sign_off_boq_for_project",
            Area: "BoQ",
            Description: "SIGNS OFF a project's Bill of Quantities — a real commercial action "
                + "freezing the tender total at sign-off as the baseline record. Directors only.",
            CommandType: typeof(SignOffBoqForProject),
            ResultType: typeof(BoqSignOff),
            AuthorisationType: typeof(SignOffBoqForProjectAuthorisation),
            ValidationType: typeof(SignOffBoqForProjectValidation),
            VisibleTo: BoqSignOffDirectors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling. signedOffByEmail names the signer on the "
                + "record and tenderTotalAtSignOff must match the BoQ's current total — the "
                + "endpoint takes both from the caller, so state them explicitly."),

        // ── Retention ─────────────────────────────────────────────────────────────────────────

    };
}
