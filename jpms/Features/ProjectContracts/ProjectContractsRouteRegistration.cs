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

        commands.Register<SetProjectContractTerms, ProjectContract>(
            new CommandRoute("PUT", "/api/projects/{projectId}/contract",
                command => $"/api/projects/{((SetProjectContractTerms)command).ProjectId}/contract"));

        // The document upload is multipart/form-data and is posted directly by
        // HttpProjectContractStore, not through the JSON command sender, so
        // AttachProjectContractDocument is intentionally not registered here. Same treatment as
        // UploadDrawingRevision.
    }
}
