using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

public sealed record PostRequestMessage(
    string RequestId,
    string Body,
    MessageVisibility Visibility,
    string AuthorEmail,
    string AuthorName,
    // The in-app message this one replies to; null posts a top-level message. The handler
    // rejects a parent that doesn't exist on the same request.
    string? ParentMessageId = null) : ICommand<RequestMessage>;
