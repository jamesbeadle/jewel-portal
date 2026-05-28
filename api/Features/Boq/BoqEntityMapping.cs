using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Boq;

internal static class BoqEntityMapping
{
    public static BoqLineItem ToModel(this BoqLineItemEntity entity) => new(
        BoqLineItemId: entity.BoqLineItemId,
        ProjectId: entity.ProjectId,
        Description: entity.Description,
        Unit: entity.Unit,
        Quantity: entity.Quantity,
        RateValue: entity.RateValue,
        CostCode: entity.CostCode,
        Discipline: (Discipline)entity.Discipline);

    public static BoqSignOff ToModel(this BoqSignOffEntity entity) => new(
        BoqSignOffId: entity.BoqSignOffId,
        ProjectId: entity.ProjectId,
        SignedOffByEmail: entity.SignedOffByEmail,
        SignedOffAt: entity.SignedOffAt,
        TenderTotalAtSignOff: entity.TenderTotalAtSignOff);
}
