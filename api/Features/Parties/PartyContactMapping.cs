using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Parties;

internal static class PartyContactMapping
{
    public static PartyContact ToModel(this PartyContactEntity entity) => new(
        PartyContactId: entity.PartyContactId,
        PartyKind: (PartyKind)entity.PartyKind,
        PartyId: entity.PartyId,
        Name: entity.Name,
        Email: entity.Email,
        JobTitle: entity.JobTitle,
        DefaultRouting: (CorrespondenceRouting)entity.DefaultRouting,
        IsPrimary: entity.IsPrimary,
        CreatedAt: entity.CreatedAt);

    public static string NextId() => Guid.NewGuid().ToString("N");

    /// <summary>Parses the route segment ("client" / "architect") a party-contacts URL carries.</summary>
    public static PartyKind? ParsePartyKind(string? segment) => segment?.ToLowerInvariant() switch
    {
        "client"    => PartyKind.Client,
        "architect" => PartyKind.Architect,
        _ => null
    };
}
