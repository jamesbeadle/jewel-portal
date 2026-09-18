using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class CommercialActions
{
    private static IEnumerable<AiAction> StatementActions() => new AiAction[]
    {
        // ── Commercial: the valuation statement ──────────────────────────────────────────
        // Since 2026-09-18 the claim IS the statement: locking it (preapprove_valuation_claim)
        // freezes the lines, and delete_valuation_claim removes them. The retired
        // take_/delete_valuation_report_snapshot actions had nothing left to do.

        new AiAction(
            Name: "send_valuation_statement_email",
            Area: "Commercial",
            Description: "SENDS EMAIL: sends a LOCKED valuation's statement from the shared projects "
                + "mailbox to the project's Client and Architect contacts, attached as a PDF, tagged "
                + "to the valuation (JPMS/VAL-…). saveAsDraftOnly true stops after staging, leaving "
                + "the reviewed draft in Drafts for Outlook instead of sending. The subject and HTML "
                + "cover note are the caller's. A Draft valuation is refused — lock it first.",
            CommandType: typeof(SendValuationStatementEmail),
            ResultType: typeof(ValuationStatementEmailOutcome),
            AuthorisationType: typeof(SendValuationStatementEmailAuthorisation),
            ValidationType: typeof(SendValuationStatementEmailValidation),
            VisibleTo: StatementEmailSenders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "This is client-facing money correspondence that reaches the client the moment "
                + "this succeeds — ALWAYS confirm the valuation, the subject and the cover-note wording "
                + "with the user before calling (preview_record_email, recordType valuation_claim, "
                + "shows exactly what will go). Recipients are fixed to the project's Client and "
                + "Architect contacts (projects@ is cc'd automatically). valuationClaimId comes from "
                + "list_valuations / get_valuation_context; a retired snapshot id is accepted and "
                + "resolves to its claim. The result's draftMessageId is the handle for "
                + "delete_mailbox_draft if a staged draft has to be withdrawn."),
    };
}
