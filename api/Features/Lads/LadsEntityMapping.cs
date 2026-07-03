using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Lads;

internal static class LadsEntityMapping
{
    public static LadClaim ToModel(this LadClaimEntity entity) =>
        new(entity.LadClaimId,
            entity.ProjectId,
            entity.Reference,
            entity.Title,
            entity.Description,
            entity.PeriodFrom,
            entity.PeriodTo,
            entity.DaysClaimed,
            entity.RatePerWeek,
            entity.Amount,
            (LadStatus)entity.Status,
            entity.RaisedAt,
            entity.CreatedByEmail);
}
