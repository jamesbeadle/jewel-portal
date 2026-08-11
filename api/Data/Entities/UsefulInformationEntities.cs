using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A Useful Information note: a titled piece of free text kept against a project for the office's
// own use — door codes, key safe locations, access arrangements, site quirks. Strictly internal
// reference material (UsefulInformationRoles gates reads AND writes to internal roles), so unlike
// the record families there is no sequential reference and no mailbox tag — a note is never
// corresponded about, it is just looked up. House style: loose string ids, no FK constraints.
public sealed class UsefulInformationNoteEntity
{
    [Key, MaxLength(64)] public string UsefulInformationNoteId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    // Free text. Same ceiling as RequestMessageEntity.Body — plenty for a door code or a page of
    // site notes, small enough to stay an in-row nvarchar.
    [MaxLength(4000)]    public string Body { get; set; } = "";
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    // Last edit, stamped by UpdateUsefulInformationNoteHandler; null = never edited.
    [MaxLength(256)]     public string? UpdatedByEmail { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
