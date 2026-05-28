using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Mobilisation;

internal static class MobilisationEntityMapping
{
    public static MobilisationItem ToModel(this MobilisationItemEntity entity) =>
        new(entity.MobilisationItemId, entity.ProjectId, entity.Description, entity.OwnerEmail, entity.IsComplete, entity.CompletedAt);
}
