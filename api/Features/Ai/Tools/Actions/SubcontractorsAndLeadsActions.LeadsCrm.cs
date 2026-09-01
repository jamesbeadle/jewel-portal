using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Architects.Commands;
using Jewel.JPMS.Api.Features.Clients;
using Jewel.JPMS.Api.Features.Clients.Commands;
using Jewel.JPMS.Api.Features.Directory.Commands;
using Jewel.JPMS.Api.Features.Leads.Commands;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SubcontractorsAndLeadsActions
{
    private static IEnumerable<AiAction> LeadsCrmActions() => new AiAction[]
    {
        new AiAction(
            Name: "capture_lead",
            Area: "Leads & CRM",
            Description: "Creates a new lead in the CRM pipeline with its contact, site address, "
                + "estimated value, source and owner.",
            CommandType: typeof(CaptureLead),
            ResultType: typeof(Lead),
            AuthorisationType: typeof(CaptureLeadAuthorisation),
            ValidationType: typeof(CaptureLeadValidation),
            VisibleTo: LeadWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "ownerEmail is the portal email of the staff member who owns the lead — usually the "
                + "signed-in user unless they say otherwise; it becomes the project manager if the lead "
                + "is won."),

        new AiAction(
            Name: "update_lead_details",
            Area: "Leads & CRM",
            Description: "Updates a lead's details — reference, contact, company, site address, estimated "
                + "value, source, pipeline stage and owner. Sends the whole record: every field is "
                + "applied as supplied.",
            CommandType: typeof(UpdateLeadDetails),
            ResultType: typeof(Lead),
            AuthorisationType: typeof(UpdateLeadDetailsAuthorisation),
            ValidationType: typeof(UpdateLeadDetailsValidation),
            VisibleTo: LeadWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Read the lead first and carry forward the fields that should not change. leadId comes "
                + "from the pipeline listing."),

        new AiAction(
            Name: "record_lead_qualification_score",
            Area: "Leads & CRM",
            Description: "Records a qualification assessment on a lead — a score and notes on whether it "
                + "is worth pursuing.",
            CommandType: typeof(RecordLeadQualificationScore),
            ResultType: typeof(QualificationAssessment),
            AuthorisationType: typeof(RecordLeadQualificationScoreAuthorisation),
            ValidationType: typeof(RecordLeadQualificationScoreValidation),
            VisibleTo: LeadWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "assessedByEmail is the portal email of the assessor — the signed-in user unless they "
                + "say otherwise."),

        new AiAction(
            Name: "book_site_visit",
            Area: "Leads & CRM",
            Description: "Books a site visit on a lead for a scheduled date and time with a list of "
                + "attendee emails. This records the visit in the CRM — it does not send calendar "
                + "invitations or email anyone.",
            CommandType: typeof(BookSiteVisit),
            ResultType: typeof(SiteVisit),
            AuthorisationType: typeof(BookSiteVisitAuthorisation),
            ValidationType: typeof(BookSiteVisitValidation),
            VisibleTo: LeadWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "scheduledAt is ISO 8601."),

        new AiAction(
            Name: "record_site_visit_notes",
            Area: "Leads & CRM",
            Description: "Records the notes, photo count and completion flag on a booked site visit — "
                + "replacing what is there.",
            CommandType: typeof(RecordSiteVisitNotes),
            ResultType: typeof(SiteVisit),
            AuthorisationType: typeof(RecordSiteVisitNotesAuthorisation),
            ValidationType: typeof(RecordSiteVisitNotesValidation),
            VisibleTo: RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "siteVisitId comes from the lead's site-visit list (ListSiteVisitsForLead)."),

        new AiAction(
            Name: "record_information_chase_item",
            Area: "Leads & CRM",
            Description: "Records an item of information being chased on a lead (drawings, survey, "
                + "budget…) and whether it has been received.",
            CommandType: typeof(RecordInformationChaseItem),
            ResultType: typeof(InfoChaseItem),
            AuthorisationType: typeof(RecordInformationChaseItemAuthorisation),
            ValidationType: typeof(RecordInformationChaseItemValidation),
            VisibleTo: LeadWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "record_bid_decision",
            Area: "Leads & CRM",
            Description: "Records the bid/no-bid decision on a lead with the reasoning — the gate that "
                + "decides whether the lead is estimated.",
            CommandType: typeof(RecordBidDecision),
            ResultType: typeof(BidDecision),
            AuthorisationType: typeof(RecordBidDecisionAuthorisation),
            ValidationType: typeof(RecordBidDecisionValidation),
            VisibleTo: LeadDeciders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the decision and reason with the user before calling. decidedByEmail is the "
                + "portal email of the decision maker — the signed-in user unless they say otherwise."),

        new AiAction(
            Name: "issue_proposal",
            Area: "Leads & CRM",
            Description: "Records the proposal issued on a lead at a value. This records it in the CRM — "
                + "it does not generate or send a proposal document. Refused if the lead already has a "
                + "proposal (use revise_proposal instead).",
            CommandType: typeof(IssueProposal),
            ResultType: typeof(Proposal),
            AuthorisationType: typeof(IssueProposalAuthorisation),
            ValidationType: typeof(IssueProposalValidation),
            VisibleTo: LeadWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "revise_proposal",
            Area: "Leads & CRM",
            Description: "Records a negotiation round on a lead's existing proposal — the revised value "
                + "and notes are appended to the proposal's history.",
            CommandType: typeof(ReviseProposal),
            ResultType: typeof(Proposal),
            AuthorisationType: typeof(ReviseProposalAuthorisation),
            ValidationType: typeof(ReviseProposalValidation),
            VisibleTo: LeadWorkers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "mark_lead_as_won",
            Area: "Leads & CRM",
            Description: "CREATES A NEW PROJECT: marks a lead won and immediately creates a project shell "
                + "from it (reference and client from the lead, the lead's owner as project manager). The "
                + "lead moves to the Won stage.",
            CommandType: typeof(MarkLeadAsWon),
            ResultType: typeof(LeadOutcome),
            AuthorisationType: typeof(MarkLeadAsWonAuthorisation),
            ValidationType: typeof(MarkLeadAsWonValidation),
            VisibleTo: LeadDeciders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — a project is created as a side effect. "
                + "decidedByEmail is the portal email of the decision maker — the signed-in user unless "
                + "they say otherwise."),

        new AiAction(
            Name: "mark_lead_as_lost",
            Area: "Leads & CRM",
            Description: "Marks a lead lost with the reason. The lead leaves the active pipeline.",
            CommandType: typeof(MarkLeadAsLost),
            ResultType: typeof(LeadOutcome),
            AuthorisationType: typeof(MarkLeadAsLostAuthorisation),
            ValidationType: typeof(MarkLeadAsLostValidation),
            VisibleTo: LeadDeciders,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the reason with the user before calling. decidedByEmail is the portal email "
                + "of the decision maker — the signed-in user unless they say otherwise."),

        // ── Contacts (client accounts, architect practices, party contact books) ──────────────

    };
}
