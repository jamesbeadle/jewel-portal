using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

// Remove one workflow tag from an email — unlinking it from that process. If it was the email's last
// tag, the marker is dropped too and the email returns to the triage queue. InternetMessageId lets
// the operation re-find the message if its Graph id has changed since the list was rendered.
public sealed record RemoveTagFromMessage(
    string MessageId,
    string? InternetMessageId,
    string Tag) : ICommand<Acknowledgement>;
