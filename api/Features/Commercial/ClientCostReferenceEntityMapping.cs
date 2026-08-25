using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial;

internal static class ClientCostReferenceEntityMapping
{
    public static ClientCostReference ToModel(this ClientCostReferenceEntity entity) =>
        new(entity.ClientCostReferenceId, entity.ProjectId, entity.CostCode, entity.ClientReference);
}
