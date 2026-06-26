using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests;

// Builds the conversation entry that records an ingested email against a request. The message
// is always inbound + shared (it came from an external participant and belongs in the shared
// thread), carries the email's threading identifiers so later replies can be stitched back to
// it, and is timestamped with the email's own received time so the thread reads in true order.
internal static class IntakeConversation
{
    public static RequestMessageEntity AsInboundMessage(IntakeEmailEntity intake, string requestId) => new()
    {
        MessageId = RequestsIdentifierFactory.Next(),
        RequestId = requestId,
        AuthorEmail = intake.FromEmail,
        AuthorName = string.IsNullOrWhiteSpace(intake.FromName) ? intake.FromEmail : intake.FromName,
        Body = string.IsNullOrWhiteSpace(intake.BodyPreview) ? "(no message body)" : intake.BodyPreview,
        Visibility = (int)MessageVisibility.Shared,
        PostedAt = intake.ReceivedAt,
        Direction = (int)MessageDirection.Inbound,
        EmailMessageId = intake.InternetMessageId,
        InReplyTo = intake.InReplyTo,
        ConversationId = intake.ConversationId,
        SentStatus = (int)MessageSentStatus.NotApplicable
    };
}
