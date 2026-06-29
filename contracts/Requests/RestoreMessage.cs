using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

// Undo a discard: move the message from the "General" folder back into the Inbox, where it re-enters
// the triage queue. InternetMessageId lets the move re-find the message if its Graph id has changed.
public sealed record RestoreMessage(
    string MessageId,
    string? InternetMessageId = null) : ICommand<Acknowledgement>;
