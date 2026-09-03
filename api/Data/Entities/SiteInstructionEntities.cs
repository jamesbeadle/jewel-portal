using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A site instruction on a project — a written instruction to site (what, where) under a short
// title. Raised on the project's Site Instructions page or from an email in the Control Centre
// (the Internal pathway's "create new", 2026-09-03 — the email alone is rarely the instruction,
// so the triager writes it); either way its SI-#### reference is also its mailbox tag stem, so
// its emails read back live by tag — the same arrangement as defects and inventory.
public sealed class SiteInstructionEntity
{
    [Key, MaxLength(64)] public string SiteInstructionId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(4000)]    public string Instruction { get; set; } = "";
    [MaxLength(256)]     public string Location { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    // Sequential, human-readable number (rendered as SI-0001). Global — like defect, to-do and
    // inventory numbers — so the tag stem is unique across the flat JPMS mailbox-category space.
    // Minted by AddSiteInstructionHandler.
    public int Number { get; set; }

    // The canonical reference this instruction's emails are tagged with ("SI-0001" ->
    // "JPMS/SI-0001"). Computed, not stored. The id-derived fallback covers any unnumbered row
    // (there should be none) so two such rows can never share the "SI-0000" stem.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"SI-{Number:0000}"
        : $"SI-{SiteInstructionId.PadRight(8, '0')[..8].ToUpperInvariant()}";
}
