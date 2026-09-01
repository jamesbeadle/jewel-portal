using Jewel.JPMS.Api.Features.Boq.Commands;
using Jewel.JPMS.Api.Features.Lads;
using Jewel.JPMS.Api.Features.Lads.Commands;
using Jewel.JPMS.Api.Features.Retention.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class VariationsAndValuationsActions
{
    private static IEnumerable<AiAction> LadsActions() => new AiAction[]
    {
        new AiAction(
            Name: "add_lad_claim",
            Area: "LADs",
            Description: "Records a Liquidated Damages claim the client has notified against the "
                + "project — period, days claimed, rate per week and amount, created in the "
                + "Notified state. Recorded as created by the signed-in user.",
            CommandType: typeof(AddLadClaim),
            ResultType: typeof(LadClaim),
            AuthorisationType: typeof(AddLadClaimAuthorisation),
            ValidationType: typeof(AddLadClaimValidation),
            VisibleTo: LadRoles.AllowedToManageLads,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. raisedAt is the date of the client's "
                + "notice; left null it defaults to now."),

        new AiAction(
            Name: "update_lad_claim",
            Area: "LADs",
            Description: "Updates a recorded LADs claim — its commercial details and its status as "
                + "the claim moves through Notified → Disputed / Agreed / Withdrawn / Settled. "
                + "Marking a claim Agreed or Settled is a real commercial position; the whole "
                + "record is re-stated each call.",
            CommandType: typeof(UpdateLadClaim),
            ResultType: typeof(LadClaim),
            AuthorisationType: typeof(UpdateLadClaimAuthorisation),
            ValidationType: typeof(UpdateLadClaimValidation),
            VisibleTo: LadRoles.AllowedToManageLads,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Read the claim first and carry forward every field that should not change — "
                + "the command replaces the record. Confirm status changes with the user."),
    };
}
