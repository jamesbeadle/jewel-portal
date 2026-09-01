using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.ClientPortal;

namespace Jewel.JPMS.Api.Features.ClientPortal;

// The client-facing projections. These are the ONLY shapes a client session ever receives for a
// request or variation, so what is absent here is absent from the portal by construction:
// internal notes, request values, quoting machinery, mailbox metadata.
internal static class ClientPortalEntityMapping
{
    public static ClientPortalRequest ToClientModel(this RequestEntity entity, string projectName) => new(
        RequestId: entity.RequestId,
        ProjectId: entity.ProjectId,
        ProjectName: projectName,
        Reference: entity.Reference,
        Title: entity.Title,
        Description: entity.Description,
        Kind: (RequestType)entity.Kind,
        Status: (RequestStatus)entity.Status,
        RaisedAt: entity.RaisedAt,
        IssuedAt: entity.IssuedAt,
        RespondedAt: entity.RespondedAt,
        ResponseText: entity.ResponseText);

    public static ClientPortalVariationOrder ToClientModel(this VariationOrderEntity entity, string projectName) => new(
        VariationOrderId: entity.VariationOrderId,
        ProjectId: entity.ProjectId,
        ProjectName: projectName,
        Reference: entity.Reference,
        VariationRef: entity.VariationRef,
        Title: entity.Title,
        Description: entity.Description,
        Status: (VariationOrderStatus)entity.Status,
        EstimatedValue: entity.EstimatedValue,
        Value: entity.Value,
        CreatedAt: entity.CreatedAt,
        IssuedAt: entity.IssuedAt,
        ApprovedAt: entity.ApprovedAt);
}
