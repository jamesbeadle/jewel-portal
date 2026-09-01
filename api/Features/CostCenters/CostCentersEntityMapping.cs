using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.CostCenters;

public static class CostCentersEntityMapping
{
    public static CostCenter ToModel(this CostCenterEntity entity) => new(
        entity.CostCenterId,
        entity.Code,
        entity.Name,
        entity.SortOrder,
        entity.IsActive);
}
