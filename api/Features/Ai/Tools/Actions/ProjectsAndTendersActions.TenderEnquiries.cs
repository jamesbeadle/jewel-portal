using Jewel.JPMS.Api.Features.ArchitectInstructions;
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
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProjectsAndTendersActions
{
    private static IEnumerable<AiAction> TenderEnquiriesActions() => new AiAction[]
    {
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

    };
}
