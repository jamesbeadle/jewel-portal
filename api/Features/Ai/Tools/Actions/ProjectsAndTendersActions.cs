using Jewel.JPMS.Api.Features.BuildingControl;
using Jewel.JPMS.Api.Features.BuildingControl.Attachments;
using Jewel.JPMS.Api.Features.BuildingControl.Commands;
using Jewel.JPMS.Api.Features.Mobilisation.Commands;
using Jewel.JPMS.Api.Features.ProjectContracts;
using Jewel.JPMS.Api.Features.ProjectContracts.Commands;
using Jewel.JPMS.Api.Features.Projects.Commands;
using Jewel.JPMS.Api.Features.Projects.Contacts;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.TenderEnquiries;
using Jewel.JPMS.Api.Features.TenderEnquiries.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Tender enquiry, project, project-contract, mobilisation and building control commands
/// as connector actions. Mirrors Features/TenderEnquiries, Features/Projects,
/// Features/ProjectContracts, Features/Mobilisation and Features/BuildingControl — each entry's
/// VisibleTo copies its Authorisation class's role set (replicated inline where the endpoint keeps
/// the set in a private field), and the stamps copy exactly what the endpoint stamps server-side.
/// Follows CalendarActions, the exemplar file.</summary>
internal sealed class ProjectsAndTendersActions : IAiActionSource
{
    // Replicas of role sets held as PRIVATE fields inside the authorisation classes they mirror —
    // kept identical to the source lists named in each entry's AuthorisationType.

    // CreateProjectShellAuthorisation.RolesThatMayCreateProjects.
    private static readonly RoleSet ProjectCreators =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    // UpdateProjectDetailsAuthorisation / SetNextValuationDateAuthorisation /
    // SetExpectedMonthlyValuationAuthorisation — all three hold the same RolesThatMayUpdateProjects.
    private static readonly RoleSet ProjectEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    // DeleteProjectAuthorisation.RolesThatMayDeleteProjects — deliberately narrower than the editors.
    private static readonly RoleSet ProjectDeleters =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    // ProjectContactAuthorisation.RolesThatMayManageContacts.
    private static readonly RoleSet ProjectContactManagers =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);

    // UpdateMobilisationChecklistItemAuthorisation.RolesThatMayUpdateMobilisation.
    private static readonly RoleSet MobilisationEditors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead);

    public IEnumerable<AiAction> Build() => new[]
    {
        // ── Tender enquiries ──────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "log_tender_enquiry",
            Area: "Tender enquiries",
            Description: "Logs a tender enquiry by hand (the phone-call case) — no email, no files. "
                + "CAN CREATE A NEW PROJECT: exactly one of projectId (an existing project) or "
                + "newProject (a Lead-stage shell the handler creates, with the architect as its "
                + "correspondent party) must be given. Recorded as logged by the signed-in user.",
            CommandType: typeof(LogTenderEnquiry),
            ResultType: typeof(TenderEnquiry),
            AuthorisationType: typeof(LogTenderEnquiryAuthorisation),
            ValidationType: typeof(LogTenderEnquiryValidation),
            VisibleTo: TenderEnquiryRoles.Managers,
            EmailStamps: new[] { "LoggedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. When newProject is supplied a project is "
                + "created as a side effect — confirm with the user before calling. Dates in details "
                + "are ISO 8601 calendar dates."),

        new AiAction(
            Name: "log_tender_enquiry_from_message",
            Area: "Tender enquiries",
            Description: "Turns an architect's invitation email into a tender enquiry record and tags "
                + "the email thread to it (triage pathway). CAN CREATE A NEW PROJECT: exactly one of "
                + "projectId or newProject (a Lead-stage shell) is given. The ticked email attachments "
                + "(the PQQ, the drawings) are copied mailbox → blob store server-side.",
            CommandType: typeof(LogTenderEnquiryFromMessage),
            ResultType: typeof(TenderEnquiry),
            AuthorisationType: typeof(LogTenderEnquiryFromMessageAuthorisation),
            ValidationType: typeof(LogTenderEnquiryFromMessageValidation),
            VisibleTo: TenderEnquiryRoles.Managers,
            EmailStamps: new[] { "LoggedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue, not a request id. "
                + "Refused if the thread already carries another pathway unless allowCrossPathway is "
                + "true — pass that only with the user's explicit say-so. A vanished attachment fails "
                + "cleanly before anything persists."),

        new AiAction(
            Name: "update_tender_enquiry_details",
            Area: "Tender enquiries",
            Description: "Replaces a tender enquiry's editable details wholesale — title, architect "
                + "practice and contact, scope summary, contract form, received/PQQ-due/tender-due "
                + "dates. Read the enquiry first and carry forward what should not change.",
            CommandType: typeof(UpdateTenderEnquiryDetails),
            ResultType: typeof(TenderEnquiry),
            AuthorisationType: typeof(UpdateTenderEnquiryDetailsAuthorisation),
            ValidationType: typeof(UpdateTenderEnquiryDetailsValidation),
            VisibleTo: TenderEnquiryRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "tenderEnquiryId comes from the enquiry register (get_tender_enquiry_context / "
                + "find_by_reference)."),

        new AiAction(
            Name: "set_tender_enquiry_status",
            Area: "Tender enquiries",
            Description: "Moves a tender enquiry along its journey (Received, Declined, PqqSubmitted, "
                + "Shortlisted, NotShortlisted, TenderSubmitted, Won, Lost). The handler stamps the "
                + "matching date (PQQ submitted, tender submitted, decided) and refuses a status the "
                + "current one cannot reach.",
            CommandType: typeof(SetTenderEnquiryStatus),
            ResultType: typeof(TenderEnquiry),
            AuthorisationType: typeof(SetTenderEnquiryStatusAuthorisation),
            ValidationType: typeof(SetTenderEnquiryStatusValidation),
            // Broadest set the gate admits: bookkeeping moves take Managers; the decision statuses
            // (Declined, Won, Lost) are allowed only to TenderEnquiryRoles.Deciders (director / PM).
            VisibleTo: TenderEnquiryRoles.Managers,
            EmailStamps: new[] { "ChangedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm with the user before calling — a status move is a matter of record, and "
                + "Declined/Won/Lost are bid decisions restricted to a director or project manager. "
                + "Further per-command checks apply at execution."),

        new AiAction(
            Name: "set_tender_enquiry_answers",
            Area: "Tender enquiries",
            Description: "Replaces a tender enquiry's questionnaire (PQQ) answers wholesale — the "
                + "whole sheet is saved in one write and positions are re-minted 1..n from the order "
                + "the rows arrive in. Read the current answers first and carry forward what should "
                + "not change.",
            CommandType: typeof(SetTenderEnquiryAnswers),
            ResultType: typeof(IReadOnlyList<TenderEnquiryAnswer>),
            AuthorisationType: typeof(SetTenderEnquiryAnswersAuthorisation),
            ValidationType: typeof(SetTenderEnquiryAnswersValidation),
            VisibleTo: TenderEnquiryRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        // ── Projects ──────────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "create_project_shell",
            Area: "Projects",
            Description: "Creates a new project with its reference, name, client, organisation, "
                + "project manager and stage — it appears across the portal immediately.",
            CommandType: typeof(CreateProjectShell),
            ResultType: typeof(Project),
            AuthorisationType: typeof(CreateProjectShellAuthorisation),
            ValidationType: typeof(CreateProjectShellValidation),
            VisibleTo: ProjectCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectManagerEmail is a portal user's email. Confirm the reference with the "
                + "user — it is how the whole team will know the job."),

        new AiAction(
            Name: "update_project_details",
            Area: "Projects",
            Description: "Overwrites a project's details wholesale — reference, name, client, "
                + "organisation, stage, project manager, correspondent party, site address and Xero "
                + "site name. Fields omitted are not kept: read the project first and carry forward "
                + "everything that should not change. The party assignment decides where project "
                + "emails (RFIs and other request documents) are addressed.",
            CommandType: typeof(UpdateProjectDetails),
            ResultType: typeof(Project),
            AuthorisationType: typeof(UpdateProjectDetailsAuthorisation),
            ValidationType: typeof(UpdateProjectDetailsValidation),
            VisibleTo: ProjectEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Echo current values for anything unchanged — "
                + "a null partyId clears the party assignment."),

        new AiAction(
            Name: "delete_project",
            Area: "Projects",
            Description: "PERMANENTLY DELETES a project and every record filed under it — requests, "
                + "variations, valuations, programme, drawings register, financial records, the lot — "
                + "in one transaction. There is no undo. Xero ledger lines are not deleted; their "
                + "allocation to the project is cleared, returning them to the unallocated queue. The "
                + "audit trail survives, gaining a ProjectDeleted event.",
            CommandType: typeof(DeleteProject),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteProjectAuthorisation),
            ValidationType: typeof(DeleteProjectValidation),
            VisibleTo: ProjectDeleters,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user, naming the project, before calling. "
                + "confirmName must match the project's name exactly — the server re-checks it and "
                + "refuses a mismatch."),

        new AiAction(
            Name: "set_next_valuation_date",
            Area: "Projects",
            Description: "Sets (or clears, with null) the date the next valuation is expected on a "
                + "project — the one field, without round-tripping the full project details.",
            CommandType: typeof(SetNextValuationDate),
            ResultType: typeof(Project),
            AuthorisationType: typeof(SetNextValuationDateAuthorisation),
            ValidationType: null,
            VisibleTo: ProjectEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. The date is ISO 8601."),

        new AiAction(
            Name: "set_expected_monthly_valuation",
            Area: "Projects",
            Description: "Sets (or clears, with null) the forecast assumption of roughly how much the "
                + "architect is expected to certify per valuation month on a project. Forecasting "
                + "only — the Cash Forecast claims left-to-claim at this rate; it never touches "
                + "valuations or invoices.",
            CommandType: typeof(SetExpectedMonthlyValuation),
            ResultType: typeof(Project),
            AuthorisationType: typeof(SetExpectedMonthlyValuationAuthorisation),
            ValidationType: null,
            VisibleTo: ProjectEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "expectedMonthlyValuation must be a positive amount, or null to clear — zero or "
                + "negative is not a rate."),

        new AiAction(
            Name: "upsert_project_contact",
            Area: "Projects",
            Description: "Adds or updates a person on a project's correspondence profile, including "
                + "how they join issued request documents (To/Cc/Bcc/None) — this decides who receives "
                + "the project's outbound request correspondence. A null/blank contactId inserts; a "
                + "populated one updates in place.",
            CommandType: typeof(UpsertProjectContact),
            ResultType: typeof(ProjectContact),
            AuthorisationType: typeof(ProjectContactAuthorisation),
            ValidationType: typeof(UpsertProjectContactValidation),
            VisibleTo: ProjectContactManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "contactId for an update comes from the project's contact list "
                + "(ListProjectContacts). partyContactId, when set, links the row to a person on the "
                + "party's contact book as a per-project routing override."),

        new AiAction(
            Name: "remove_project_contact",
            Area: "Projects",
            Description: "Deletes a person from a project's correspondence profile permanently — they "
                + "stop receiving the project's outbound request correspondence. There is no undo.",
            CommandType: typeof(RemoveProjectContact),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ProjectContactAuthorisation),
            ValidationType: null,
            VisibleTo: ProjectContactManagers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which contact, by name and project, before calling. "
                + "contactId comes from the project's contact list (ListProjectContacts)."),

        // ── Project contracts ─────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "set_project_contract_terms",
            Area: "Project contracts",
            Description: "Records or replaces a project's contract terms in one write — form, parties, "
                + "contract sum, LADs, key dates, retention, notice periods and the commercial "
                + "percentages. Upsert: one contract per project, created on first call. These figures "
                + "are the basis of every valuation, notice and variation argument on the project. The "
                + "uploaded contract document is untouched, by design.",
            CommandType: typeof(SetProjectContractTerms),
            ResultType: typeof(ProjectContract),
            AuthorisationType: typeof(SetProjectContractTermsAuthorisation),
            ValidationType: typeof(SetProjectContractTermsValidation),
            VisibleTo: ProjectContractRoles.AllowedToManageContract,
            EmailStamps: new[] { "UpdatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "The write replaces the whole terms record — read the current contract first "
                + "(get_project_contract) and carry forward every field that should not change. "
                + "Confirm changed figures with the user before calling."),

        new AiAction(
            Name: "set_project_contract_amendment_details",
            Area: "Project contracts",
            Description: "Corrects the title, date or notes on a recorded contract amendment. The "
                + "amendment's document is untouched, by design — a wrong file is fixed by removing "
                + "the amendment and uploading again in the portal.",
            CommandType: typeof(SetProjectContractAmendmentDetails),
            ResultType: typeof(ProjectContractAmendment),
            AuthorisationType: typeof(SetProjectContractAmendmentDetailsAuthorisation),
            ValidationType: typeof(SetProjectContractAmendmentDetailsValidation),
            VisibleTo: ProjectContractRoles.AllowedToManageContract,
            EmailStamps: new[] { "UpdatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectContractAmendmentId comes from the contract's amendment list "
                + "(ListProjectContractAmendments)."),

        new AiAction(
            Name: "remove_project_contract_amendment",
            Area: "Project contracts",
            Description: "PERMANENTLY REMOVES one contract amendment and its stored document (the "
                + "signed deed file). There is no undo.",
            CommandType: typeof(RemoveProjectContractAmendment),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveProjectContractAmendmentAuthorisation),
            ValidationType: typeof(RemoveProjectContractAmendmentValidation),
            VisibleTo: ProjectContractRoles.AllowedToManageContract,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Irreversible. Confirm with the user which amendment, by title and date, before "
                + "calling. projectContractAmendmentId comes from ListProjectContractAmendments."),

        // ── Mobilisation ──────────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "update_mobilisation_checklist_item",
            Area: "Mobilisation",
            Description: "Updates one item on a project's mobilisation checklist in a single write — "
                + "its description, owner and complete/incomplete state together. Send all three: the "
                + "command replaces the item's editable fields wholesale.",
            CommandType: typeof(UpdateMobilisationChecklistItem),
            ResultType: typeof(MobilisationItem),
            AuthorisationType: typeof(UpdateMobilisationChecklistItemAuthorisation),
            ValidationType: typeof(UpdateMobilisationChecklistItemValidation),
            VisibleTo: MobilisationEditors,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "mobilisationItemId comes from the project's mobilisation checklist "
                + "(GetMobilisationChecklistForProject). Read the item first and carry forward what "
                + "should not change."),

        // ── Building control ──────────────────────────────────────────────────────────────────

        new AiAction(
            Name: "create_building_control_case",
            Area: "Building control",
            Description: "Sets up a project's building control case — regime, body, references, "
                + "contact and dates. With seedStandardStages true (the default) the standard "
                + "inspection checklist is planted as Planned stages, freely edited afterwards. "
                + "Refused while the project already has an active case — mark that one Lapsed first.",
            CommandType: typeof(CreateBuildingControlCase),
            ResultType: typeof(BuildingControlCase),
            AuthorisationType: typeof(CreateBuildingControlCaseAuthorisation),
            ValidationType: typeof(CreateBuildingControlCaseValidation),
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Dates are UK-local calendar dates."),

        new AiAction(
            Name: "update_building_control_case",
            Area: "Building control",
            Description: "Replaces a building control case's details wholesale — regime, body, "
                + "references, contact, notice/acceptance dates and notes. A NoticeSubmitted case "
                + "whose acceptance date has just been entered moves to In force automatically.",
            CommandType: typeof(UpdateBuildingControlCase),
            ResultType: typeof(BuildingControlCase),
            AuthorisationType: typeof(UpdateBuildingControlCaseAuthorisation),
            ValidationType: typeof(UpdateBuildingControlCaseValidation),
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "buildingControlCaseId comes from the project's Building Control tab data "
                + "(GetBuildingControlForProject). Read the case first and carry forward what should "
                + "not change."),

        new AiAction(
            Name: "set_building_control_case_status",
            Area: "Building control",
            Description: "Moves a building control case along its ladder (NoticeSubmitted, InForce, "
                + "CompletionRequested, CompletionCertified, Lapsed). Moving to CompletionCertified "
                + "stamps the certificate date (today unless one is passed); moving away clears it.",
            CommandType: typeof(SetBuildingControlCaseStatus),
            ResultType: typeof(BuildingControlCase),
            AuthorisationType: typeof(SetBuildingControlCaseStatusAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Confirm the move with the user before calling — the case status is where the "
                + "project's sign-off stands."),

        new AiAction(
            Name: "add_building_control_inspection",
            Area: "Building control",
            Description: "Adds one inspection stage to a building control case, at the foot of the "
                + "running order. A stage with a bookedFor date starts at Booked, otherwise Planned.",
            CommandType: typeof(AddBuildingControlInspection),
            ResultType: typeof(BuildingControlInspection),
            AuthorisationType: typeof(AddBuildingControlInspectionAuthorisation),
            ValidationType: typeof(AddBuildingControlInspectionValidation),
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: new[] { "RaisedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "buildingControlCaseId comes from GetBuildingControlForProject. bookedFor is the "
                + "official date agreed with the inspector; inspectedAt is when the visit actually "
                + "happened."),

        new AiAction(
            Name: "update_building_control_inspection",
            Area: "Building control",
            Description: "Replaces an inspection stage's details wholesale — stage name, booked and "
                + "inspected dates, outcome notes and inspector name.",
            CommandType: typeof(UpdateBuildingControlInspection),
            ResultType: typeof(BuildingControlInspection),
            AuthorisationType: typeof(UpdateBuildingControlInspectionAuthorisation),
            ValidationType: typeof(UpdateBuildingControlInspectionValidation),
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "buildingControlInspectionId comes from GetBuildingControlForProject. Read the "
                + "inspection first and carry forward what should not change."),

        new AiAction(
            Name: "set_building_control_inspection_status",
            Area: "Building control",
            Description: "Moves an inspection along its ladder (Planned, Booked, Inspected, Passed, "
                + "ActionsRequired, Closed — and back, when a booking falls through). Moving to "
                + "Inspected stamps the inspected date (today unless the inspection already carries "
                + "one); moving back to Planned/Booked clears it.",
            CommandType: typeof(SetBuildingControlInspectionStatus),
            ResultType: typeof(BuildingControlInspection),
            AuthorisationType: typeof(SetBuildingControlInspectionStatusAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_building_control_inspection",
            Area: "Building control",
            Description: "Deletes an inspection stage permanently — only a Planned stage with no "
                + "files; anything booked, inspected or carrying evidence is refused (close it "
                + "instead). There is no undo, and the stage's BCI number is never re-issued.",
            CommandType: typeof(DeleteBuildingControlInspection),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteBuildingControlInspectionAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which stage, by name, before calling."),

        new AiAction(
            Name: "create_building_control_inspection_from_message",
            Area: "Building control",
            Description: "Turns an inspector's email — a booking confirmation, a visit arrangement — "
                + "into an inspection stage on the project's building control case and tags the email "
                + "thread to it (triage pathway). Requires the case to exist already.",
            CommandType: typeof(CreateBuildingControlInspectionFromMessage),
            ResultType: typeof(BuildingControlInspection),
            AuthorisationType: typeof(CreateBuildingControlInspectionFromMessageAuthorisation),
            ValidationType: typeof(CreateBuildingControlInspectionFromMessageValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue, not a request id. "
                + "Refused if the thread already carries another pathway unless allowCrossPathway is "
                + "true — pass that only with the user's explicit say-so."),

        new AiAction(
            Name: "set_building_control_attachment_kind",
            Area: "Building control",
            Description: "Re-kinds a stored building control file (Photo, SiteInspectionReport, "
                + "Notice, Acknowledgement, DecisionNotice, PlanningPermission, CompletionCertificate, "
                + "Other) — the row is the record, the bytes never move.",
            CommandType: typeof(SetBuildingControlAttachmentKind),
            ResultType: typeof(BuildingControlAttachment),
            AuthorisationType: typeof(SetBuildingControlAttachmentKindAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "buildingControlAttachmentId comes from the case's or inspection's attachment "
                + "list."),

        new AiAction(
            Name: "remove_building_control_attachment",
            Area: "Building control",
            Description: "Deletes a stored building control file permanently — the row first, the "
                + "blob best-effort. There is no undo.",
            CommandType: typeof(RemoveBuildingControlAttachment),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(RemoveBuildingControlAttachmentAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which file, by name, before calling."),

        new AiAction(
            Name: "copy_email_attachments_to_building_control_inspection",
            Area: "Building control",
            Description: "Copies files off an email linked to an inspection — the inspector's site "
                + "report, their photos — into the inspection's attachment store, so the evidence "
                + "lives with the record rather than only in the thread. Kind is inferred per file "
                + "when not passed: image → Photo, PDF → SiteInspectionReport, anything else → Other.",
            CommandType: typeof(CopyEmailAttachmentsToBuildingControlInspection),
            ResultType: typeof(IReadOnlyList<BuildingControlAttachment>),
            AuthorisationType: typeof(CopyEmailAttachmentsToBuildingControlInspectionAuthorisation),
            ValidationType: null,
            VisibleTo: BuildingControlRoles.Managers,
            EmailStamps: new[] { "AddedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId and attachmentIds come from the emails linked to the inspection "
                + "(read_record_emails)."),
    };

    // Skipped: UploadTenderEnquiryAttachments — multipart/form-data file upload, no JSON command dispatch.
    // Skipped: RemoveTenderEnquiryAttachment — no Authorisation class: the endpoint gates inline on TenderEnquiryRoles.Managers, and AiAction requires a DI-resolvable authorisation class with Allows.
    // Skipped: AttachProjectContractDocument (UploadProjectContractDocumentEndpoint) — multipart/form-data upload; the command is server-constructed after the blob is stored.
    // Skipped: AttachProjectContractAmendment (UploadProjectContractAmendmentEndpoint) — multipart/form-data upload; the command is server-constructed after the blob is stored.
    // Skipped: AcceptMyWorkOrder (Portal) — no command dispatch: the endpoint writes the acceptance directly through JpmsContext.
    // Skipped: RaiseMyVariationRequest (Portal) — no Authorisation class: the endpoint gates inline on the session's SubcontractorScope, and AiAction requires a DI-resolvable authorisation class.
    // Skipped: WithdrawMyVariationRequest (Portal) — no command dispatch: the endpoint updates the row directly through JpmsContext.
    // Skipped: AddComplianceDocumentVersion (Portal, UploadMyComplianceDocumentEndpoint) — multipart/form-data upload; the command is server-constructed after the blob is stored.
    // Skipped: UploadBuildingControlCaseAttachments / UploadBuildingControlInspectionAttachments — multipart/form-data uploads, no command dispatch.
    // Skipped: RecordArchitectInstruction — multipart/form-data (the instruction document travels with the form) AND no Authorisation class (inline ArchitectInstructionRoles.AllowedToManage gate).
    // Skipped: ImportArchitectInstructionFromMessage — no Authorisation class: inline ArchitectInstructionRoles.AllowedToManage gate.
    // Skipped: UpdateArchitectInstruction — no Authorisation class: inline ArchitectInstructionRoles.AllowedToManage gate.
    // Skipped: LinkArchitectInstructionToVariation — no Authorisation class: inline ArchitectInstructionRoles.AllowedToManage gate.
    // Skipped: UnlinkArchitectInstructionFromVariation — no Authorisation class: inline ArchitectInstructionRoles.AllowedToManage gate.
    // Skipped: DeleteArchitectInstruction — no Authorisation class: inline ArchitectInstructionRoles.AllowedToManage gate.
    // (Features/Places has no command endpoints at all — LocalBusinessSearch and WebsiteContactFinder are query-side services.)
}
