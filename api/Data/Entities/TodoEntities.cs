using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A project to-do item. Rows are created from the project's Overview tab or from an email at the
// triage stage. The sequential Number renders as "TODO-0001", which doubles as the mailbox tag stem
// ("JPMS/TODO-0001") — the link between an item and its emails is the tag, never a stored copy.
public sealed class TodoItemEntity
{
    [Key, MaxLength(64)] public string TodoItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Notes { get; set; } = "";

    // The ROLE the item is assigned to (a Models.Role value stored as int, same convention as
    // DirectoryUserRoleEntity.Role; null = unassigned). Items belong to a role, not a person, so
    // they survive staff changes: whoever holds the role sees them, and a new starter taking over
    // the role inherits the open items with no re-assignment.
    public int? AssigneeRole { get; set; }

    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public bool IsComplete { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Sequential, human-readable item number (rendered as TODO-0001). Global — like request and bid
    // package numbers — so the tag stem is unique across the flat JPMS mailbox-category space.
    public int Number { get; set; }

    // The canonical reference this item's emails are tagged with ("TODO-0001" -> "JPMS/TODO-0001").
    // Computed, not stored.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => $"TODO-{Number:0000}";
}
