using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// A file the user attached to the assistant chat — the bridge for work still living in the boss's
/// spreadsheets: attach the sheet, open the create dialog, and the assistant populates the form
/// from it instead of anyone retyping.
///
/// <para>The bytes travel base64 in the command body (no multipart, no second auth path) and are
/// NOT stored: the server extracts the contents to text ONCE and persists that as a Context row on
/// the conversation, so every subsequent hop replays it to the model and a refresh loses nothing.
/// A null <see cref="ConversationId"/> starts a new conversation, exactly like SendAiMessage.</para>
/// </summary>
public sealed record AddAiAttachment(
    string? ConversationId,
    string FileName,
    string ContentBase64,
    AiScope Scope,
    string SentByEmail) : ICommand<AiAttachmentReceipt>;

/// <summary>What the panel shows once the file has been read: the (possibly new) conversation and a
/// one-line human summary — "2 sheets · 148 rows", "3,412 characters".</summary>
public sealed record AiAttachmentReceipt(
    string ConversationId,
    string FileName,
    string Summary);
