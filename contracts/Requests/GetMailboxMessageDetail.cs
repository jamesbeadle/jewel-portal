using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Full body + attachment list for one mailbox message, read live from Graph when a triager opens it.
// InternetMessageId is carried so the message can be re-found if its Graph id has changed since the
// list was rendered.
public sealed record GetMailboxMessageDetail(
    string MessageId,
    string? InternetMessageId = null) : IQuery<MailboxMessageDetail>;
