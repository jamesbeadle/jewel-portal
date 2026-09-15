using Jewel.JPMS.Api.Features.Sales;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The proposals on a lead over the connector (2026-09-15, Nigel: "ingest the emails and
/// attachments and prepare a proposal"). A proposal is the document the PROSPECT sees on their
/// imagine page — scope, base price, priced options, a schedule of works, terms — as distinct
/// from the estimate, which is Jewel's own pricing. The assistant drafts it from the enquiry
/// (read_record_emails record_type lead, read_email_attachment) and the estimate; a person
/// reads the draft on the lead's page before anything is sent.
/// </summary>
internal sealed partial class SalesActions
{
    private static IEnumerable<AiAction> ProposalActions() => new AiAction[]
    {
        new AiAction(
            Name: "save_sales_proposal",
            Area: Area,
            Description: "Saves a proposal DRAFT on a lead — a new version when proposalId is blank, "
                + "otherwise the named unsent draft rewritten. The prospect sees nothing until "
                + "send_sales_proposal; a sent proposal is never edited — save a new version.",
            CommandType: typeof(SaveSalesProposal),
            ResultType: typeof(SalesProposal),
            AuthorisationType: typeof(SaveSalesProposalAuthorisation),
            ValidationType: typeof(SaveSalesProposalValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: new[] { "SavedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "leadId is the lead's id (get_lead / find_by_reference LD-####); proposalId "
                + "blank for a new version, else get_lead's proposals[].proposalId of a Draft. "
                + "Draft from what the enquiry actually says — read_record_emails record_type lead "
                + "and read_email_attachment for the drawings and specification — and from the "
                + "lead's estimate (get_lead estimates[]): basePrice is the estimate's total unless "
                + "the user says otherwise. Fields: title; scope (Markdown — what is included, the "
                + "specification, exclusions); basePrice (£); options[] of {name, description, "
                + "priceDelta (£ over the base), recommended (pre-ticked)}; schedule[] of {name, "
                + "startWeek (1 = the first week), weeks}; terms (Markdown — payment terms, "
                + "validity, the contract terms accepted on acceptance); heroImageId (an imagine "
                + "render's id, optional). Show the user the draft before saving it."),

        new AiAction(
            Name: "send_sales_proposal",
            Area: Area,
            Description: "Sends a draft proposal: it becomes the lead's live proposal (an earlier "
                + "Sent one is Superseded), the PROSPECT IS EMAILED the link to their imagine page "
                + "where it shows, the lead moves to Proposal and the timeline records it. Needs "
                + "the lead to hold an imagine link and a contact email — refused otherwise.",
            CommandType: typeof(SendSalesProposal),
            ResultType: typeof(SalesProposal),
            AuthorisationType: typeof(SendSalesProposalAuthorisation),
            ValidationType: typeof(SendSalesProposalValidation),
            VisibleTo: SalesRoles.SalesTeam,
            EmailStamps: new[] { "SentByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "External-facing and irreversible from here: read the draft back to the user — "
                + "title, base price, options, schedule, terms, who it goes to — and get their "
                + "explicit yes first. proposalId from get_lead proposals[]; note is an optional "
                + "line for the timeline.",
            RequiresConfirmation: true),

        new AiAction(
            Name: "withdraw_sales_proposal",
            Area: Area,
            Description: "Withdraws a SENT proposal (Superseded) without sending another. Directors "
                + "and the Finance Director only.",
            CommandType: typeof(WithdrawSalesProposal),
            ResultType: typeof(SalesProposal),
            AuthorisationType: typeof(WithdrawSalesProposalAuthorisation),
            ValidationType: typeof(WithdrawSalesProposalValidation),
            VisibleTo: SalesRoles.Deciders,
            EmailStamps: new[] { "DecidedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "proposalId from get_lead proposals[] — a Sent one.",
            RequiresConfirmation: true)
    };
}
