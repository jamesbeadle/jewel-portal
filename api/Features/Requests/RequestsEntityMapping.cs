using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests;

internal static class RequestsEntityMapping
{
    public static Request ToModel(this RequestEntity entity) => new(
        RequestId: entity.RequestId,
        ProjectId: entity.ProjectId,
        Kind: (RequestType)entity.Kind,
        Reference: entity.Reference,
        Title: entity.Title,
        Description: entity.Description,
        Status: (RequestStatus)entity.Status,
        Value: entity.Value,
        RaisedByEmail: entity.RaisedByEmail,
        RaisedAt: entity.RaisedAt,
        RespondedAt: entity.RespondedAt,
        ResponseText: entity.ResponseText,
        RespondedByEmail: entity.RespondedByEmail,
        ImpliesVariation: entity.ImpliesVariation,
        RaisedTo: entity.RaisedTo,
        DrawingRef: entity.DrawingRef,
        ResponseDue: entity.ResponseDue,
        RelatedDrawingSpec: entity.RelatedDrawingSpec,
        InternalNotes: entity.InternalNotes,
        ClientNotes: entity.ClientNotes);
}
