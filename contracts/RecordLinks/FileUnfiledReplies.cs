using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.RecordLinks;

// The connector's "File them all here" (the file_unfiled_replies action): files every reply
// ListUnfiledReplies currently reports for the record — the newer thread members not yet tagged
// to it — exactly as the record page's banner button does: one LinkMessageToRecord per reply,
// MessageOnly scope, so untagged thread siblings keep queueing for their own triage decisions.
// No HTTP endpoint — the portal page loops LinkMessageToRecord itself (UnfiledRepliesNotice);
// an AI caller gets the loop server-side with a per-reply outcome instead of a silent partial.
public sealed record FileUnfiledReplies(
    RecordType Type,
    string RecordId) : ICommand<FileUnfiledRepliesResult>;

/// <summary>One unfiled reply's outcome: filed, or refused with the handler's own reason
/// (e.g. a cross-pathway conflict) — a refusal never stops the rest.</summary>
public sealed record FiledReplyOutcome(
    string Subject,
    string FromName,
    string FromEmail,
    DateTimeOffset ReceivedAt,
    bool Filed,
    string? Error = null);

public sealed record FileUnfiledRepliesResult(
    int Found,
    int Filed,
    IReadOnlyList<FiledReplyOutcome> Replies);
