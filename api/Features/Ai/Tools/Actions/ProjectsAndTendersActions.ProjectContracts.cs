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
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProjectsAndTendersActions
{
    private static IEnumerable<AiAction> ProjectContractsActions() => new AiAction[]
    {
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

    };
}
