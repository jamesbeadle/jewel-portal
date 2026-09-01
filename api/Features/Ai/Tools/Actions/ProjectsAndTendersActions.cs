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
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Tender enquiry, project, project-contract, mobilisation and building control commands
/// as connector actions. Mirrors Features/TenderEnquiries, Features/Projects,
/// Features/ProjectContracts, Features/Mobilisation and Features/BuildingControl — each entry's
/// VisibleTo copies its Authorisation class's role set (replicated inline where the endpoint keeps
/// the set in a private field), and the stamps copy exactly what the endpoint stamps server-side.
/// Follows CalendarActions, the exemplar file.</summary>
internal sealed partial class ProjectsAndTendersActions : IAiActionSource
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

    public IEnumerable<AiAction> Build() =>
        TenderEnquiriesActions()
            .Concat(ProjectsActions())
            .Concat(ProjectContractsActions())
            .Concat(MobilisationActions())
            .Concat(BuildingControlActions())
            .Concat(ArchitectInstructionsActions());

    // Skipped: UploadTenderEnquiryAttachments — multipart/form-data file upload, no JSON command dispatch.
    // Skipped: RemoveTenderEnquiryAttachment — no Authorisation class: the endpoint gates inline on TenderEnquiryRoles.Managers, and AiAction requires a DI-resolvable authorisation class with Allows.
    // Skipped: AttachProjectContractDocument (UploadProjectContractDocumentEndpoint) — multipart/form-data upload; the command is server-constructed after the blob is stored.
    // Skipped: AttachProjectContractAmendment (UploadProjectContractAmendmentEndpoint) — multipart/form-data upload; the command is server-constructed after the blob is stored.
    // Skipped: AcceptMyWorkOrder (Portal) — no command dispatch: the endpoint writes the acceptance directly through JpmsContext.
    // Skipped: RaiseMyVariationRequest (Portal) — no Authorisation class: the endpoint gates inline on the session's SubcontractorScope, and AiAction requires a DI-resolvable authorisation class.
    // Skipped: WithdrawMyVariationRequest (Portal) — no command dispatch: the endpoint updates the row directly through JpmsContext.
    // Skipped: AddComplianceDocumentVersion (Portal, UploadMyComplianceDocumentEndpoint) — multipart/form-data upload; the command is server-constructed after the blob is stored.
    // Skipped: UploadBuildingControlCaseAttachments / UploadBuildingControlInspectionAttachments — multipart/form-data uploads, no command dispatch.
    // Skipped: RecordArchitectInstruction — multipart/form-data: the instruction document travels with the form; import_architect_instruction_from_message covers the from-email path.
    // (The other five architect-instruction commands are no longer skipped — gate classes added 2026-08-31, declared in the Architect instructions area above.)
    // (Features/Places has no command endpoints at all — LocalBusinessSearch and WebsiteContactFinder are query-side services.)
}
