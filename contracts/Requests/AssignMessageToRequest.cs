using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

// Assign a mailbox message to an existing request. The handler records the email as an inbound,
// shared message on the request's conversation (carrying the threading ids) and moves the message
// out of the Inbox into the request's folder. No intake row — the mailbox is the source of truth.
public sealed record AssignMessageToRequest(
    string MessageId,
    string RequestId,
    string? InternetMessageId = null) : ICommand<Acknowledgement>;
