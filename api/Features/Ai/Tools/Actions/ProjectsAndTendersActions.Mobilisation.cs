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
    private static IEnumerable<AiAction> MobilisationActions() => new AiAction[]
    {
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

    };
}
