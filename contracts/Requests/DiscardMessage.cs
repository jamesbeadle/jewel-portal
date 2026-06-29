using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

// Discard a mailbox message: it isn't a request (spam, an auto-reply, a misdirected email), so move
// it out of the Inbox into the "General" folder. No database row — the mailbox is the source of
// truth. InternetMessageId lets the move re-find the message if its Graph id has gone stale.
public sealed record DiscardMessage(
    string MessageId,
    string? InternetMessageId = null) : ICommand<Acknowledgement>;
