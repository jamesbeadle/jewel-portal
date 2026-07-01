using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Clients;

internal static class ClientEntityMapping
{
    public static Client ToModel(this ClientEntity entity) => new(
        ClientId: entity.ClientId,
        Name: entity.Name,
        PrimaryContactName: entity.PrimaryContactName,
        PrimaryContactEmail: entity.PrimaryContactEmail,
        ArchitectName: entity.ArchitectName,
        ArchitectEmail: entity.ArchitectEmail,
        CreatedAt: entity.CreatedAt);
}
