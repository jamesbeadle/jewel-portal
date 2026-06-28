using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Projects.Contacts;

internal static class ProjectContactMapping
{
    public static ProjectContact ToModel(this ProjectContactEntity entity) => new(
        ContactId: entity.ContactId,
        ProjectId: entity.ProjectId,
        Name: entity.Name,
        Email: entity.Email,
        Organisation: entity.Organisation,
        Role: (ProjectContactRole)entity.Role,
        ReceivesRequests: entity.ReceivesRequests,
        CreatedAt: entity.CreatedAt);

    public static string NextId() => Guid.NewGuid().ToString("N");
}
