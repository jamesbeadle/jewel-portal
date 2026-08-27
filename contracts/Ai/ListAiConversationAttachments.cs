using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// The files attached to one assistant conversation, oldest first — name, type and size, never the
/// bytes. Read by the chat-aware dialogs beside the panel (the work-order form's "files from this
/// chat" list above all) so a quote the assistant drafted an order from can be kept ON the order
/// without anyone re-picking it from disk. Scoped server-side to the caller's own conversations —
/// a conversation id is not a capability, here as everywhere.
/// </summary>
public sealed record ListAiConversationAttachments(string ConversationId)
    : IQuery<IReadOnlyList<AiConversationAttachment>>;

/// <summary>One attached file's register entry. AttachmentId is what
/// <c>AttachChatFilesToWorkOrder</c> takes; the bytes stay server-side.</summary>
public sealed record AiConversationAttachment(
    string AttachmentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt)
{
    /// <summary>True for the image types the panel carries inline — usually pasted screenshots,
    /// which is why the work-order form leaves them unticked by default.</summary>
    public bool IsImage =>
        ContentType is { Length: > 0 } type
        && type.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
