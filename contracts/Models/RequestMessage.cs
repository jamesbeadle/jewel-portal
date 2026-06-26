namespace Jewel.JPMS.Models;

// Who a conversation message is visible to. Mirrors the InternalNotes / ClientNotes
// split on the Request itself: a request thread mixes internal Jewel discussion with
// messages shared out to external participants (architect, subcontractor, client).
public enum MessageVisibility
{
    Internal = 0, // Jewel staff only
    Shared = 1    // visible to external participants on the request
}

// A single entry in a request's back-and-forth conversation. Requests are long-running
// discussions, so every contribution is captured here with its author and timestamp,
// giving the auditable thread that replaces the email chains RFIs are run on today.
public sealed record RequestMessage(
    string MessageId,
    string RequestId,
    string AuthorEmail,
    string AuthorName,
    string Body,
    MessageVisibility Visibility,
    DateTimeOffset PostedAt);
