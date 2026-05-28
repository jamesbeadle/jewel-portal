using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Rates;

internal static class RateEntityMapping
{
    public static Rate ToModel(this RateEntity entity) => new(
        RateId: entity.RateId,
        Trade: entity.Trade,
        Description: entity.Description,
        Unit: entity.Unit,
        Value: entity.Value,
        SupplierName: entity.SupplierName,
        LastPricedAt: entity.LastPricedAt);
}
