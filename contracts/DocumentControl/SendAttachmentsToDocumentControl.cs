using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Copies the ticked attachments of one email out of the mailbox into Document Control: each becomes
// a Pending DocumentControlItem with the file bytes landed in the document-control blob store and
// the email's envelope snapshotted for context. Staged in the Control Centre and run by the
// email's triage Apply; like the old save-to-drawings it never consumes the email. Attachments
// already sent from this message are skipped, so a re-run Apply cannot double-send. Returns the
// items created by THIS call.
public sealed record SendAttachmentsToDocumentControl(
    string MessageId,
    string? InternetMessageId,
    IReadOnlyList<string> AttachmentIds,
    // The triage form's project at Apply time — carried as a filing hint, freely overridden later.
    string? ProjectIdHint) : ICommand<IReadOnlyList<DocumentControlItem>>;
