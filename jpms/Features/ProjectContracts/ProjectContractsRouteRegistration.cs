using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.ProjectContracts;

public static class ProjectContractsRouteRegistration
{
    public static void RegisterProjectContractsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<GetProjectContract, ProjectContract?>(
            new QueryRoute("/api/projects/{projectId}/contract",
                query => $"/api/projects/{((GetProjectContract)query).ProjectId}/contract"));

        queries.Register<ListProjectContractAmendments, IReadOnlyList<ProjectContractAmendment>>(
            new QueryRoute("/api/projects/{projectId}/contract/amendments",
                query => $"/api/projects/{((ListProjectContractAmendments)query).ProjectId}/contract/amendments"));

        commands.Register<SetProjectContractTerms, ProjectContract>(
            new CommandRoute("PUT", "/api/projects/{projectId}/contract",
                command => $"/api/projects/{((SetProjectContractTerms)command).ProjectId}/contract"));

        commands.Register<SetProjectContractAmendmentDetails, ProjectContractAmendment>(
            new CommandRoute("PUT", "/api/projects/{projectId}/contract/amendments/{amendmentId}",
                command => $"/api/projects/{((SetProjectContractAmendmentDetails)command).ProjectId}/contract/amendments/{((SetProjectContractAmendmentDetails)command).ProjectContractAmendmentId}"));

        commands.Register<RemoveProjectContractAmendment, Acknowledgement>(
            new CommandRoute("DELETE", "/api/projects/{projectId}/contract/amendments/{amendmentId}",
                command => $"/api/projects/{((RemoveProjectContractAmendment)command).ProjectId}/contract/amendments/{((RemoveProjectContractAmendment)command).ProjectContractAmendmentId}"));

        // The document uploads (executed contract and amendments) are multipart/form-data and are
        // posted directly by HttpProjectContractStore, not through the JSON command sender, so
        // AttachProjectContractDocument and AttachProjectContractAmendment are intentionally not
        // registered here. Same treatment as UploadDrawingRevision.
    }
}
