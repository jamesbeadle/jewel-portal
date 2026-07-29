using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// Full body + attachments for one email in the project's programme communications (the scheduling
// bucket, tag "JPMS/SCH-<projectRef>"), read live and sanitised server-side. The Communications
// list only carries Graph's short bodyPreview snippet — which truncates long emails and drops the
// quoted thread — so the full content is fetched here on demand when a reader expands a message.
// Scoped to the bucket: the message must currently carry the programme tag, so this cannot be used
// to read arbitrary mailbox content. InternetMessageId lets the mailbox re-find the message if its
// Graph id changed since the list was rendered.
public sealed record GetProgrammeEmailDetail(
    string ProjectId,
    string MessageId,
    string? InternetMessageId = null) : IQuery<MailboxMessageDetail>;
