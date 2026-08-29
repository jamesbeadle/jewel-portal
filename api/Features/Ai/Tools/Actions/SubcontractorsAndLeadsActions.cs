using Jewel.JPMS.Api.Features.Architects;
using Jewel.JPMS.Api.Features.Architects.Commands;
using Jewel.JPMS.Api.Features.Clients;
using Jewel.JPMS.Api.Features.Clients.Commands;
using Jewel.JPMS.Api.Features.Directory.Commands;
using Jewel.JPMS.Api.Features.Leads.Commands;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Subcontractor directory, leads/CRM, party-contact and user-directory commands as
/// connector actions. Mirrors Features/Subcontractors, Features/Leads, Features/Parties,
/// Features/Architects, Features/Clients and Features/Directory — each entry's VisibleTo copies
/// its Authorisation class's role set (replicated inline where the endpoint keeps the set in a
/// private field), and the stamps copy exactly what the endpoint stamps server-side.</summary>
internal sealed class SubcontractorsAndLeadsActions : IAiActionSource
{
    // Replicas of role sets held as PRIVATE fields inside the authorisation classes they mirror —
    // kept identical to the source lists named in each entry's AuthorisationType.
    private static readonly RoleSet DirectoryCurators =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    private static readonly RoleSet DirectoryRecordEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    private static readonly RoleSet LeadWorkers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    private static readonly RoleSet LeadDeciders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    private static readonly RoleSet PartyContactManagers =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    // AdminGate.Allows: Role.Admin or Role.FinanceDirector.
    private static readonly RoleSet UserAdministrators =
        RoleSet.Of(Role.Admin, JpmsRoles.FinanceDirector);

    public IEnumerable<AiAction> Build() => new[]
    {
        // ── Subcontractors ────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "add_subcontractor_to_directory",
            Area: "Subcontractors",
            Description: "Creates a new company record in the subcontractor/supplier directory with its "
                + "trades, primary contact, CIS status, payment terms and postal address. With isProspect "
                + "true the record is minted for a bid-package tender list only and stays out of the "
                + "Directory until promoted.",
            CommandType: typeof(AddSubcontractorToDirectory),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(AddSubcontractorToDirectoryAuthorisation),
            ValidationType: typeof(AddSubcontractorToDirectoryValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "tradeIds come from list_trades (add_trade can mint a missing one). Check "
                + "search_directory for an existing record first — duplicates are merged later with "
                + "consolidate_directory_records, so avoid creating them."),

        new AiAction(
            Name: "update_subcontractor",
            Area: "Subcontractors",
            Description: "Updates a directory record's company name, trades, primary contact, CIS status, "
                + "payment terms and address. Null paymentTermsDays or address fields mean \"leave "
                + "unchanged\"; an empty string clears a field.",
            CommandType: typeof(UpdateSubcontractor),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(UpdateSubcontractorAuthorisation),
            ValidationType: typeof(UpdateSubcontractorValidation),
            VisibleTo: DirectoryRecordEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "subcontractorId comes from search_directory, which also returns the record's current "
                + "trades — send the full trade list back, removing the last trade is refused. Never "
                + "guess or derive the id (a Xero contact id is NOT a directory id)."),

        new AiAction(
            Name: "promote_subcontractor_to_directory",
            Area: "Subcontractors",
            Description: "Promotes a tender-only prospect record into the Directory proper — the "
                + "deliberate \"this company is worth keeping\" act. Idempotent: promoting a record "
                + "already in the directory returns it unchanged.",
            CommandType: typeof(PromoteSubcontractorToDirectory),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(PromoteSubcontractorToDirectoryAuthorisation),
            ValidationType: typeof(PromoteSubcontractorToDirectoryValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "consolidate_directory_records",
            Area: "Subcontractors",
            Description: "MERGES duplicate directory records into one master and PERMANENTLY DELETES the "
                + "merged-away records. Applies the supplied winning field values to the master, unions "
                + "trades, re-points everything that referenced a merged record (work orders, tenders, "
                + "compliance documents, portal logins, Xero links…) and keeps losing contact details as "
                + "company contact rows. There is no undo.",
            CommandType: typeof(ConsolidateDirectoryRecords),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(ConsolidateDirectoryRecordsAuthorisation),
            ValidationType: typeof(ConsolidateDirectoryRecordsValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user exactly which record is the master, which are "
                + "merged away, and each winning field value before calling. mergedSubcontractorIds must "
                + "never include the master."),

        new AiAction(
            Name: "import_xero_supplier",
            Area: "Subcontractors",
            Description: "Copies one Xero supplier into the company directory as a new record (category "
                + "Supplier, no trades) linked to the Xero contact; Xero's additional contact persons "
                + "become company contact rows. Never merges into an existing record — duplicates are "
                + "resolved afterwards with consolidate_directory_records.",
            CommandType: typeof(ImportXeroSupplier),
            ResultType: typeof(Subcontractor),
            AuthorisationType: typeof(ImportXeroSupplierAuthorisation),
            ValidationType: typeof(ImportXeroSupplierValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "xeroContactId is Xero's contact id. Refused if the supplier is already imported or "
                + "Xero is unreachable. The import is recorded against the signed-in user."),

        new AiAction(
            Name: "upsert_company_contact",
            Area: "Subcontractors",
            Description: "Adds or updates a person on a directory record's contact list, with the "
                + "free-text purpose the contact serves (\"Accounts\", \"Projects\", \"Estimating\"…). A "
                + "null/blank companyContactId inserts; a populated one updates in place.",
            CommandType: typeof(UpsertCompanyContact),
            ResultType: typeof(CompanyContact),
            AuthorisationType: typeof(UpsertCompanyContactAuthorisation),
            ValidationType: typeof(UpsertCompanyContactValidation),
            VisibleTo: DirectoryRecordEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "companyContactId for an update comes from the record's contact list "
                + "(ListCompanyContacts)."),

        new AiAction(
            Name: "remove_company_contact",
            Area: "Subcontractors",
            Description: "Deletes a person from a directory record's contact list permanently. There is "
                + "no undo.",
            CommandType: typeof(RemoveCompanyContact),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(UpsertCompanyContactAuthorisation),
            ValidationType: null,
            VisibleTo: DirectoryRecordEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which contact, by name and company, before calling. "
                + "companyContactId comes from the record's contact list (ListCompanyContacts)."),

        new AiAction(
            Name: "upload_compliance_document",
            Area: "Subcontractors",
            Description: "Records a compliance document entry (metadata only — no file bytes travel "
                + "through this action) against a directory record: the document kind, file name and "
                + "expiry date. Recording an existing kind supersedes the previous version rather than "
                + "duplicating it.",
            CommandType: typeof(UploadComplianceDocument),
            ResultType: typeof(ComplianceDocument),
            AuthorisationType: typeof(UploadComplianceDocumentAuthorisation),
            ValidationType: typeof(UploadComplianceDocumentValidation),
            VisibleTo: RoleSet.Of(
                JpmsRoles.Director, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin,
                JpmsRoles.Subcontractor),
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "A subcontractor portal login may only record against its own company's record — "
                + "further per-record checks apply at execution. The actual file, if there is one, is "
                + "uploaded in the portal; use this only to log a document's existence and expiry."),

        new AiAction(
            Name: "add_trade",
            Area: "Subcontractors",
            Description: "Adds a trade to the curated master trade list. The name is normalised (trimmed, "
                + "first letter capitalised) and matched case-insensitively — adding an existing trade "
                + "returns it unchanged.",
            CommandType: typeof(AddTrade),
            ResultType: typeof(Trade),
            AuthorisationType: typeof(AddTradeAuthorisation),
            ValidationType: typeof(AddTradeValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "rename_trade",
            Area: "Subcontractors",
            Description: "Renames a trade on the curated master list — every directory record carrying "
                + "the trade shows the new name at once (bid packages keep the snapshot name they were "
                + "created with). Renaming to a name another trade already holds is refused.",
            CommandType: typeof(RenameTrade),
            ResultType: typeof(Trade),
            AuthorisationType: typeof(RenameTradeAuthorisation),
            ValidationType: typeof(RenameTradeValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "tradeId comes from list_trades."),

        new AiAction(
            Name: "delete_trade",
            Area: "Subcontractors",
            Description: "Deletes a trade from the curated master list permanently. Refused while any "
                + "directory record still carries the trade — reassign those records first.",
            CommandType: typeof(DeleteTrade),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteTradeAuthorisation),
            ValidationType: typeof(DeleteTradeValidation),
            VisibleTo: DirectoryCurators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which trade, by name, before calling. tradeId comes from "
                + "list_trades."),

        // ── Leads & CRM ───────────────────────────────────────────────────────────────────────

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

        new AiAction(
            Name: "create_client",
            Area: "Contacts",
            Description: "Creates a global client account. The primary contact email captured here is "
                + "where request documents are addressed when this client is the selected party on a "
                + "project/request.",
            CommandType: typeof(CreateClient),
            ResultType: typeof(Client),
            AuthorisationType: typeof(CreateClientAuthorisation),
            ValidationType: typeof(CreateClientValidation),
            VisibleTo: ClientRoles.AllowedToManageClients,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true),

        new AiAction(
            Name: "update_client_contact",
            Area: "Contacts",
            Description: "Updates a client account's name and primary contact — changing where request "
                + "documents are addressed when this client is a project's party.",
            CommandType: typeof(UpdateClientContact),
            ResultType: typeof(Client),
            AuthorisationType: typeof(UpdateClientContactAuthorisation),
            ValidationType: typeof(UpdateClientContactValidation),
            VisibleTo: ClientRoles.AllowedToManageClients,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "clientId comes from the client listing (ListClients)."),

        new AiAction(
            Name: "create_architect",
            Area: "Contacts",
            Description: "Creates a global architect practice. The contact email captured here is where "
                + "RFIs and other request documents are addressed when this architect is the selected "
                + "party on a project/request.",
            CommandType: typeof(CreateArchitect),
            ResultType: typeof(Architect),
            AuthorisationType: typeof(CreateArchitectAuthorisation),
            ValidationType: typeof(CreateArchitectValidation),
            VisibleTo: ArchitectRoles.AllowedToManageArchitects,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true),

        new AiAction(
            Name: "update_architect",
            Area: "Contacts",
            Description: "Updates an architect practice's name and contact — changing where RFIs are "
                + "issued when this architect is a project's party.",
            CommandType: typeof(UpdateArchitect),
            ResultType: typeof(Architect),
            AuthorisationType: typeof(UpdateArchitectAuthorisation),
            ValidationType: typeof(UpdateArchitectValidation),
            VisibleTo: ArchitectRoles.AllowedToManageArchitects,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "architectId comes from the architect listing (ListArchitects)."),

        new AiAction(
            Name: "upsert_party_contact",
            Area: "Contacts",
            Description: "Adds or updates a person on a client's or architect's contact book, including "
                + "their default correspondence routing — this decides who receives Jewel's outbound "
                + "request correspondence for that party. Marking a contact primary makes them the "
                + "party's To correspondent (any previous primary is demoted).",
            CommandType: typeof(UpsertPartyContact),
            ResultType: typeof(PartyContact),
            AuthorisationType: typeof(PartyContactAuthorisation),
            ValidationType: typeof(UpsertPartyContactValidation),
            VisibleTo: PartyContactManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "partyKind is Client or Architect; partyId is the matching client/architect id "
                + "(ListClients / ListArchitects). A null/blank partyContactId inserts; a populated one "
                + "(from ListPartyContacts) updates in place."),

        new AiAction(
            Name: "remove_party_contact",
            Area: "Contacts",
            Description: "Deletes a person from a client's or architect's contact book permanently — they "
                + "stop receiving Jewel's outbound request correspondence for that party. There is no "
                + "undo.",
            CommandType: typeof(RemovePartyContact),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(PartyContactAuthorisation),
            ValidationType: null,
            VisibleTo: PartyContactManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which contact, by name and party, before calling. "
                + "partyContactId comes from ListPartyContacts."),

        // ── Directory & users ─────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "upsert_directory_user",
            Area: "Directory & users",
            Description: "Creates or updates a portal user account and REPLACES their role list with "
                + "exactly the roles supplied — this is how portal permissions are granted and taken "
                + "away. Roles omitted from the list are removed. Creating a user does not send an "
                + "invitation email.",
            CommandType: typeof(UpsertDirectoryUser),
            ResultType: typeof(DirectoryUser),
            AuthorisationType: typeof(UpsertDirectoryUserAuthorisation),
            ValidationType: typeof(UpsertDirectoryUserValidation),
            VisibleTo: UserAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm the exact role list with the user before calling — read the current user "
                + "first and carry forward roles that should not change. The Admin role carries every "
                + "permission."),

        new AiAction(
            Name: "remove_directory_user",
            Area: "Directory & users",
            Description: "REVOKES a user's portal access immediately — they can no longer sign in and "
                + "disappear from every active-user list. A soft removal: the record and roles survive "
                + "and restore_directory_user can reinstate them. Revoking the last active administrator "
                + "is refused.",
            CommandType: typeof(RemoveDirectoryUser),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveDirectoryUserAuthorisation),
            ValidationType: typeof(RemoveDirectoryUserValidation),
            VisibleTo: UserAdministrators,
            EmailStamps: new[] { "RevokedBy" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user which account, by email, before calling."),

        new AiAction(
            Name: "restore_directory_user",
            Area: "Directory & users",
            Description: "Reinstates a revoked user's portal access: their directory record, the roles "
                + "they held at revocation, and their ability to sign in (with their existing password, "
                + "or a fresh invite if they never set one).",
            CommandType: typeof(RestoreDirectoryUser),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RestoreDirectoryUserAuthorisation),
            ValidationType: typeof(RestoreDirectoryUserValidation),
            VisibleTo: UserAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "The email comes from the revoked-users list (ListRevokedDirectoryUsers). Restoring "
                + "gives the account back every role it held — confirm with the user before calling."),

        new AiAction(
            Name: "delete_directory_user",
            Area: "Directory & users",
            Description: "PERMANENTLY DELETES a revoked user's record — directory row, roles, credential, "
                + "outstanding links and sessions. Only available once the user has been revoked "
                + "(remove_directory_user first). There is no undo.",
            CommandType: typeof(DeleteDirectoryUser),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteDirectoryUserAuthorisation),
            ValidationType: typeof(DeleteDirectoryUserValidation),
            VisibleTo: UserAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user which account, by email, before calling — the "
                + "email comes from the revoked-users list (ListRevokedDirectoryUsers).")
    };

    // Skipped: InviteSubcontractorPortalUser — no command dispatch: the endpoint calls the
    //          SubcontractorPortalInviter service directly instead of an ICommandHandler.
    // Skipped: PrepareSubcontractorStatementEmailDraft — no HTTP endpoint dispatches it: the
    //          handler/authorisation/validation are registered but no [HttpTrigger] function
    //          exists for the client's /statement/draft-email route.
    // Skipped: AddComplianceDocumentVersion — constructed server-side by the multipart upload
    //          endpoints after the blob is stored; never sent by clients and has no endpoint of
    //          its own.
}
