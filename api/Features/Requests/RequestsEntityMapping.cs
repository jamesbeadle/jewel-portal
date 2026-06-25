using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Changes;

internal static class ChangesEntityMapping
{
    public static ChangeRecord ToModel(this ChangeRecordEntity entity) => new(
        ChangeRecordId: entity.ChangeRecordId,
        ProjectId: entity.ProjectId,
        Kind: (ChangeKind)entity.Kind,
        Reference: entity.Reference,
        Title: entity.Title,
        Description: entity.Description,
        Status: (ChangeStatus)entity.Status,
        Value: entity.Value,
        RaisedByEmail: entity.RaisedByEmail,
        RaisedAt: entity.RaisedAt,
        RespondedAt: entity.RespondedAt,
        ResponseText: entity.ResponseText,
        RespondedByEmail: entity.RespondedByEmail,
        ImpliesVariation: entity.ImpliesVariation);
}
