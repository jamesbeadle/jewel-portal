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
        ClientNotes: entity.ClientNotes,
        Number: entity.Number,
        HasRfq: entity.HasRfq,
        ClientId: entity.ClientId);

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

    // A mailbox email read live by tag, presented as the inbound, Shared leg of a request's
    // conversation. The body is the preview text (parity with the old snapshot); full bodies are
    // fetched on demand elsewhere. Never persisted — built fresh on every read.
    public static RequestMessage ToInboundMessage(this MailboxMessage e, string requestId) => new(
        MessageId: string.IsNullOrEmpty(e.InternetMessageId) ? e.Id : e.InternetMessageId,
        RequestId: requestId,
        AuthorEmail: e.FromEmail,
        AuthorName: string.IsNullOrWhiteSpace(e.FromName) ? e.FromEmail : e.FromName,
        Body: string.IsNullOrWhiteSpace(e.BodyPreview) ? "(no message body)" : e.BodyPreview,
        Visibility: MessageVisibility.Shared,
        PostedAt: e.ReceivedAt,
        Direction: MessageDirection.Inbound,
        EmailMessageId: e.InternetMessageId,
        InReplyTo: null,
        ConversationId: null,
        SentStatus: MessageSentStatus.NotApplicable,
        // Carried so the conversation view can expand the email to its full body on demand.
        MailboxId: e.Id);
}
