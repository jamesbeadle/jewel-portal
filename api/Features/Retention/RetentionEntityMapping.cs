using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Retention;

internal static class RetentionEntityMapping
{
    public static ProjectRetention ToModel(this ProjectRetentionEntity entity) => new(
        ProjectRetentionId: entity.ProjectRetentionId,
        ProjectId: entity.ProjectId,
        RetentionPercent: entity.RetentionPercent,
        CompletionReleasePercent: entity.CompletionReleasePercent,
        DefectsPeriodMonths: entity.DefectsPeriodMonths,
        PracticalCompletionAt: entity.PracticalCompletionAt,
        CompletionReleaseConfirmedAt: entity.CompletionReleaseConfirmedAt,
        CompletionReleaseAmount: entity.CompletionReleaseAmount,
        FinalReleaseConfirmedAt: entity.FinalReleaseConfirmedAt,
        FinalReleaseAmount: entity.FinalReleaseAmount);
}
