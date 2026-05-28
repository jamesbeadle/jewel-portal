using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Closeout;

internal static class CloseoutEntityMapping
{
    public static Defect ToModel(this DefectEntity entity) =>
        new(entity.DefectId, entity.ProjectId, entity.Description, entity.Location, entity.AssignedToEmail, (DefectStatus)entity.Status, entity.RaisedAt, entity.ResolvedAt);

    public static SettlementRecord ToModel(this SettlementRecordEntity entity) =>
        new(entity.SettlementRecordId, entity.ProjectId, entity.FinalContractValue, entity.FinalCost, entity.FinalMargin, entity.AgreedAt, entity.IsClientSigned);

    public static VatAnalysis ToModel(this VatAnalysisEntity entity) =>
        new(entity.VatAnalysisId, entity.ProjectId, entity.ZeroRatedAmount, entity.StandardRatedAmount, entity.Notes, entity.IsClientConfirmed, entity.IsArchitectConfirmed);

    public static RetentionRelease ToModel(this RetentionReleaseEntity entity) =>
        new(entity.RetentionReleaseId, entity.ProjectId, entity.Amount, entity.ReleasedAt, entity.IsPublishedDownstream);
}
