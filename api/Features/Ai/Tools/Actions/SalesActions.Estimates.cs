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
        + "budgetMentioned (£ the prospect mentioned, optional), total (£ once priced, optional — "
        + "while the breakdown has lines the total is their sum and a typed total is ignored), "
        + "notes (internal — never printed), executiveSummary (client-facing: what we understand "
        + "the project to be, how we would approach it, what is included — it opens the estimate "
        + "document), buildTime (client-facing: how long on site and when, given the start the "
        + "prospect wants), exclusions (client-facing: what the figure leaves out — VAT, "
        + "kitchens, fees, party wall…).";

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
                + "budget mentioned, total, notes, and the client-facing narrative (executive "
                + "summary, build time, exclusions). Sends the whole record: every field is "
                + "applied as supplied. Not the status (move_estimate_status) and not the priced "
                + "lines (set_estimate_breakdown). A Won or Lost estimate is history and is refused.",
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
            Name: "set_estimate_breakdown",
            Area: Area,
            Description: "Replaces an estimate's priced breakdown — the itemised sections the "
                + "estimate document prints (Preliminaries & preambles, Demolition & stripping "
                + "out, Structural steelwork, Joinery, Plumbing & heating, Electrical…), each a "
                + "list of lines: costCode, description, quantity, unit (m², m³, m, nr, item, "
                + "week, kg…), unitPrice. Line totals are quantity × unit price, computed "
                + "server-side, and the estimate's total becomes their sum. A FULL-RECORD write: "
                + "every section and line is applied as supplied and anything not sent is gone — "
                + "read the estimate first (get_lead lists estimates[].sections) and resend what should stay. "
                + "An empty sections list clears the breakdown.",
            CommandType: typeof(SetEstimateBreakdown),
            ResultType: typeof(LeadEstimate),
            AuthorisationType: typeof(SetEstimateBreakdownAuthorisation),
            ValidationType: typeof(SetEstimateBreakdownValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: new[] { "ChangedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "estimateId is the estimate's id from get_lead (estimates[].estimateId) or "
                + "find_by_reference EST-#### — never the reference. sections[]: name, provisional "
                + "(true for an allowance — the document prints it as a provisional allowance), "
                + "lines[]. costCode is optional but when given MUST be a Code from list_cost_codes, "
                + "spelled exactly (PRELIMS-SMG, STR-STL, CARP-2FX…); a blank code is allowed. "
                + "Price from the enquiry's drawings (read_email_attachment) and Jewel's rates; say "
                + "in the reply which figures are allowances. Confirm the sections and the total "
                + "with the user before writing.",
            RequiresConfirmation: true),

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
