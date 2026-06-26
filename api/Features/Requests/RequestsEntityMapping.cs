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

    public static RequestMessage ToModel(this RequestMessageEntity entity) => new(
        MessageId: entity.MessageId,
        RequestId: entity.RequestId,
        AuthorEmail: entity.AuthorEmail,
        AuthorName: entity.AuthorName,
        Body: entity.Body,
        Visibility: (MessageVisibility)entity.Visibility,
        PostedAt: entity.PostedAt,
        Direction: (MessageDirection)entity.Direction,
        EmailMessageId: entity.EmailMessageId,
        InReplyTo: entity.InReplyTo,
        ConversationId: entity.ConversationId,
        SentStatus: (MessageSentStatus)entity.SentStatus);

    public static IntakeEmail ToModel(this IntakeEmailEntity entity) => new(
        IntakeId: entity.IntakeId,
        InternetMessageId: entity.InternetMessageId,
        ConversationId: entity.ConversationId,
        InReplyTo: entity.InReplyTo,
        ReferencesHeader: entity.ReferencesHeader,
        FromEmail: entity.FromEmail,
        FromName: entity.FromName,
        Subject: entity.Subject,
        BodyPreview: entity.BodyPreview,
        HasAttachments: entity.HasAttachments,
        ReceivedAt: entity.ReceivedAt,
        Status: (IntakeStatus)entity.Status,
        ClaimedByEmail: entity.ClaimedByEmail,
        ClaimedAt: entity.ClaimedAt,
        LinkedRequestId: entity.LinkedRequestId,
        Notes: entity.Notes,
        GraphMessageId: entity.GraphMessageId);
}
