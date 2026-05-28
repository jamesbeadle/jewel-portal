using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Projects;

internal static class ProjectEntityMapping
{
    public static Project ToModel(this ProjectEntity entity) => new(
        ProjectId: entity.ProjectId,
        Reference: entity.Reference,
        Name: entity.Name,
        ClientName: entity.ClientName,
        Organisation: (Organisation)entity.Organisation,
        Stage: (ProjectStage)entity.Stage,
        ProjectManagerEmail: entity.ProjectManagerEmail,
        CreatedAt: entity.CreatedAt);
}
