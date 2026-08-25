using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A file attached to an assistant conversation, kept as bytes so the assistant can go back to
/// any part of it — the V01 tab of a forty-tab valuation, page nine of a contract — on demand.
///
/// <para>Before 2026-08-25 a chat attachment was extracted to text once, capped at 25,000
/// characters, stored on a Context row and its bytes thrown away; a multi-tab workbook lost every
/// tab after the first and nothing could go back for them. Now the bytes live in the
/// <c>ai-attachments</c> blob container (the same pattern as the drawings, Document Control and
/// contract stores), this row points at them, and the Context row carries only the manifest and a
/// short preview. See docs/ai/06-context-retrieval.md.</para>
/// </summary>
public sealed class AiAttachmentEntity
{
    [Key, MaxLength(64)] public string AttachmentId { get; set; } = "";
    [MaxLength(64)] public string ConversationId { get; set; } = "";
    [MaxLength(256)] public string FileName { get; set; } = "";
    [MaxLength(128)] public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    /// <summary>The blob's key inside the container, as the store returned it.</summary>
    [MaxLength(512)] public string BlobRef { get; set; } = "";
    /// <summary>The source manifest (kind and parts — sheets with row counts, pages, sections) as
    /// JSON, computed once at upload so listing never re-opens the file.</summary>
    public string ManifestJson { get; set; } = "";
    [MaxLength(256)] public string UploadedByEmail { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
}
