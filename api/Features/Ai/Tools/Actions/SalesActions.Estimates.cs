using Jewel.JPMS.Api.Features.Sales;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The estimates on a lead over the connector (2026-09-15, Nigel): once the enquiry is tagged to
/// its lead (SalesActions.LeadFromMessage) the assistant reads it (read_record_emails
/// record_type lead) and opens, prices and moves an estimate on the lead. Every estimate action
/// takes the estimateId (get_lead / find_by_reference hand it over), never the reference.
/// </summary>
internal sealed partial class SalesActions
{
    private const string EstimateFieldNotes =
        "Fields: scope (what is to be priced — the type of work and its extent, in the "
        + "estimator's words), architectName (the architect or consultant the enquiry came "
        + "through, if any), priceDueOn (ISO date the prospect needs the price by, optional), "
        + "budgetMentioned (£ the prospect mentioned, optional), total (£ once priced, optional), "
        + "notes.";

    private static IEnumerable<AiAction> EstimateActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_estimate",
            Area: Area,
            Description: "Opens an estimate on a lead, Received — Jewel's own pricing of one "
                + "enquiry. The EST-#### reference is minted server-side and the lead's timeline "
                + "records it. A lead may carry several: a re-price after a scope change is a new "
                + "estimate, the old one left as history.",
            CommandType: typeof(CreateEstimate),
            ResultType: typeof(LeadEstimate),
            AuthorisationType: typeof(CreateEstimateAuthorisation),
            ValidationType: typeof(CreateEstimateValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "leadId is the lead's id (get_lead / find_by_reference LD-####). Read the "
                + "enquiry first (read_record_emails record_type lead) so the scope, architect, "
                + "due date and budget come from the email, not from guesswork. " + EstimateFieldNotes),

        new AiAction(
            Name: "update_estimate_details",
            Area: Area,
            Description: "Rewrites an estimate's details — scope, architect, price due date, "
                + "budget mentioned, total, notes. Sends the whole record: every field is applied "
                + "as supplied. Not the status (move_estimate_status). A Won or Lost estimate is "
                + "history and is refused.",
            CommandType: typeof(UpdateEstimateDetails),
            ResultType: typeof(LeadEstimate),
            AuthorisationType: typeof(UpdateEstimateDetailsAuthorisation),
            ValidationType: typeof(UpdateEstimateDetailsValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "estimateId is the estimate's id from get_lead (estimates[].estimateId) or "
                + "find_by_reference EST-#### — never the reference. Read it first and carry "
                + "forward every field that should not change. " + EstimateFieldNotes),

        new AiAction(
            Name: "move_estimate_status",
            Area: Area,
            Description: "Moves an estimate along its ladder — Received → Pricing → Submitted → "
                + "Won / Lost — or back. Submitted needs a total and stamps when the price went "
                + "to the prospect; Won and Lost close it. Writes an entry on the lead's timeline "
                + "with the note if given.",
            CommandType: typeof(MoveEstimateStatus),
            ResultType: typeof(LeadEstimate),
            AuthorisationType: typeof(MoveEstimateStatusAuthorisation),
            ValidationType: typeof(MoveEstimateStatusValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: new[] { "ChangedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "estimateId is the estimate's id from get_lead (estimates[].estimateId) or "
                + "find_by_reference EST-#### — never the reference. status: Received, Pricing, "
                + "Submitted, Won, Lost. Price it (update_estimate_details total) before Submitted.",
            RequiresConfirmation: true)
    };
}
