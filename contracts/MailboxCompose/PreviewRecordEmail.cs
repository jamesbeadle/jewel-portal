using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.MailboxCompose;

/// <summary>
/// What a record's email will say, before anybody sends it.
///
/// Four records have their email composed for them — the request document (RFI/NOD/EOT), the
/// valuation statement, the subcontractor statement and the variation order — so until 2026-09-18
/// the person pressing Send had never read a word of what they were sending. That blocked a
/// director signing off the content of the portal's three highest-volume client-facing emails,
/// because the product did not display them.
///
/// It is deliberately ONE read for all four. A preview that composed its own copy of an email
/// would be the second spelling of it, and a second spelling is what put the project name in the
/// subject twice and took two sessions to find. This returns the message the door would stage.
///
/// RecipientOverride mirrors the send command's, so an ad-hoc address can be previewed before it
/// is used. Nothing else is passed in: this answers "what would go if nobody touched a word",
/// which is what seeds the two statement modals and what the assistant should read before
/// offering to send. A body a person has since edited is already in front of them.
/// </summary>
public sealed record PreviewRecordEmail(RecordType Record, string RecordId, string? RecipientOverride = null)
    : IQuery<RecordEmailPreview>;

/// <summary>
/// The email as it will leave: the subject, the body the recipient will read, who it is addressed
/// to, and what rides with it. Attachments are NAMED and sized, never carried — a preview that
/// shipped the megabytes of a rendered PDF would be a download, not a read.
/// </summary>
public sealed record RecordEmailPreview(
    RecordType Record,
    string RecordId,
    string Reference,
    string Subject,
    string BodyHtml,
    IReadOnlyList<string> To,
    IReadOnlyList<string> Cc,
    IReadOnlyList<string> Bcc,
    IReadOnlyList<PreviewedAttachment> Attachments);

/// <summary>A file the email will carry, by name and size. Bytes is what the recipient's mail
/// client will receive, so a person can see before sending that an RFI is about to carry 18 MB of
/// site photographs.</summary>
public sealed record PreviewedAttachment(string FileName, string ContentType, long Bytes);
