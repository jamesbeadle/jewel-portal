using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Architects;

internal static class ArchitectEntityMapping
{
    public static Architect ToModel(this ArchitectEntity entity) => new(
        ArchitectId: entity.ArchitectId,
        Name: entity.Name,
        ContactName: entity.ContactName,
        ContactEmail: entity.ContactEmail,
        CreatedAt: entity.CreatedAt);
}
