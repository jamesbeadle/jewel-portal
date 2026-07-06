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
        ReceivesRequests: entity.Routing == (int)CorrespondenceRouting.To,
        CreatedAt: entity.CreatedAt,
        Routing: (CorrespondenceRouting)entity.Routing,
        PartyContactId: entity.PartyContactId);

    /// <summary>A linked row rendered with the party contact's current name/email (read-through:
    /// party-level edits propagate to every project that references the person).</summary>
    public static ProjectContact ToModel(this ProjectContactEntity entity, PartyContactEntity source) => new(
        ContactId: entity.ContactId,
        ProjectId: entity.ProjectId,
        Name: source.Name,
        Email: source.Email,
        Organisation: entity.Organisation,
        Role: (ProjectContactRole)entity.Role,
        ReceivesRequests: entity.Routing == (int)CorrespondenceRouting.To,
        CreatedAt: entity.CreatedAt,
        Routing: (CorrespondenceRouting)entity.Routing,
        PartyContactId: entity.PartyContactId);

    public static string NextId() => Guid.NewGuid().ToString("N");
}
