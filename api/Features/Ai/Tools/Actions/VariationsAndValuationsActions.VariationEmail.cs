using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Api.Features.Variations.Commands;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class VariationsAndValuationsActions
{
    private static IEnumerable<AiAction> VariationEmailActions() => new AiAction[]
    {
        new AiAction(
            Name: "send_variation_order_email",
            Area: "Commercial",
            Description: "SENDS EMAIL: sends the variation order's official document from the projects "
                + "mailbox to the project's Client and Architect contacts (or one ad-hoc recipientOverride), "
                + "with the VO PDF rendered fresh from the record and attached. saveAsDraftOnly true stops "
                + "after staging, leaving the reviewed draft in Drafts for Outlook instead of sending; a "
                + "failed send leaves that same draft (outcome sent false plus a webLink). ONLY an Issued, "
                + "Awaiting AI or Approved variation may be emailed — a quoting-stage price has not been put "
                + "and a rejected one is terminal, and both are refused outright. The sent copy carries the "
                + "variation's tag, so it and the client's reply group under the variation.",
            CommandType: typeof(SendVariationOrderEmail),
            ResultType: typeof(VariationOrderEmailOutcome),
            AuthorisationType: typeof(SendVariationOrderEmailAuthorisation),
            ValidationType: typeof(SendVariationOrderEmailValidation),
            VisibleTo: VariationRoles.AllowedToManageVariations,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "The variation goes to the client the moment this succeeds, and a variation is a "
                + "request to spend — read it first (get_variation_context) and show the user the number, "
                + "the title, the value and who it will go to before calling. The document goes out under "
                + "the VO number (VO72), never the internal VOQ reference. The covering note and the "
                + "subject are composed server-side, so there is nothing to write: report them back from "
                + "the result. variationOrderId comes from list_variations or find_by_reference (V72)."),
    };
}
