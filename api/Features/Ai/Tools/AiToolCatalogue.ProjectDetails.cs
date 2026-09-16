using Jewel.JPMS.Contracts.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

public static partial class AiToolCatalogue
{
    // The Project settings page's gate (UpdateProjectDetailsAuthorisation): this read exists so
    // update_project_details has something to read first, so it is offered to the same people.
    // A property, not a static readonly field: AiToolCatalogue.All is initialised in
    // AiToolCatalogue.cs and the order static fields initialise across partial files is the
    // compiler's file order — on the Linux build this field came AFTER All, every
    // get_project_details tool carried a null VisibleTo, and ForConnector threw for every
    // caller (found 2026-09-16 by the sandbox test run).
    private static RoleSet ProjectSettingsReaders =>
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    private static IEnumerable<AiTool> ProjectDetailsTools() => new List<AiTool>
    {
        new(
            "get_project_details",
            "One project's full details, exactly as the Project settings page holds them: reference, "
            + "name, client, organisation, stage, project manager, the correspondent party (kind, "
            + "partyId, onBehalfOfClientId), the site address (addressLine, town, postcode), the Xero "
            + "site name, the Xero contact mapping (xeroContactId + xeroContactName), the next "
            + "expected valuation date, the expected monthly valuation and siteNoteSenderNames. "
            + "ALWAYS call this before update_project_details: that action overwrites every field, so "
            + "everything that should not change is echoed from here. list_projects gives only the id, "
            + "reference, name and stage.",
            AiToolSchema.Object(("projectId", "string", "The project, from list_projects. Left out, the project in view is used.", false)),
            AiToolKind.Read,
            ProjectSettingsReaders,
            async (context, input, ct) =>
            {
                var scoped = await ResolveProjectAsync(context, AiToolSchema.Text(input, "projectId"), ct);
                if (scoped is null) return NotFound("Name a project — pass projectId from list_projects (or open a project page first).");

                var project = await context.Services
                    .GetRequiredService<IQueryHandler<GetProjectById, Project?>>()
                    .HandleAsync(new GetProjectById(scoped.ProjectId), ct);
                if (project is null) return NotFound($"No project found with id {scoped.ProjectId} (list_projects returns ids).");

                return Serialise(new { ok = true, project });
            })
    };
}
