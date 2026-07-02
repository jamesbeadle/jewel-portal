using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Full body + attachment list for one email in a request's conversation, read live from Graph when
// the reader expands it. Unlike GetMailboxMessageDetail (the triage surface, gated to triage roles),
// this is scoped to a request: the server verifies the message is actually tagged to the request
// before returning anything, so conversation readers can expand emails without triage rights — and
// can't use it to read arbitrary mailbox messages. InternetMessageId is carried so the message can
// be re-found if its Graph id has changed since the conversation was rendered.
public sealed record GetRequestEmailDetail(
    string RequestId,
    string MessageId,
    string? InternetMessageId = null) : IQuery<MailboxMessageDetail>;
