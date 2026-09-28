using System.ComponentModel.DataAnnotations;
using Jewel.JPMS.Contracts.SiteInstructions;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

// A site instruction on a project — a written instruction to site (what, where) under a short
// title. Raised on the project's Site Instructions page or from an email in the Control Centre
// (the Internal pathway's "create new", 2026-09-03 — the email alone is rarely the instruction,
// so the triager writes it); either way its SI-#### reference is also its mailbox tag stem, so
// its emails read back live by tag — the same arrangement as defects and inventory.
[Index(nameof(ProgressUpdateId), Name = "IX_SiteInstructions_ProgressUpdateId")]
public sealed class SiteInstructionEntity
{
    [Key, MaxLength(64)] public string SiteInstructionId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(SiteInstructionLimits.TitleMaxLength)]    public string Title { get; set; } = "";
    public string Instruction { get; set; } = "";
    [MaxLength(SiteInstructionLimits.LocationMaxLength)] public string Location { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Given on site (2026-09-28, the worker's daily log): who gave it, on which working
    /// day, and whether it was said rather than written — a verbal direction is not an instruction
    /// until it is confirmed in writing, so the register shows it as needing that. Blank / false /
    /// null on one raised in the office. Added by AddSiteLogRaisedRecords.</summary>
    [MaxLength(SiteInstructionLimits.GivenByMaxLength)] public string GivenBy { get; set; } = "";
    public bool IsVerbal { get; set; }
    public DateTimeOffset? GivenOn { get; set; }

    /// <summary>The day's note (progress update) it was raised from, and whose log it was, when it
    /// came off a daily log. Loose string id, no FK, house style. Null / blank otherwise.</summary>
    [MaxLength(64)]  public string? ProgressUpdateId { get; set; }
    [MaxLength(256)] public string RaisedByEmail { get; set; } = "";

    // Sequential, human-readable number (rendered as SI-0001). Global — like defect, to-do and
    // inventory numbers — so the tag stem is unique across the flat JPMS mailbox-category space.
    // Minted by AddSiteInstructionHandler.
    public int Number { get; set; }

    // The canonical reference this instruction's emails are tagged with ("SI-0001" ->
    // "JPMS/SI-0001"). Computed, not stored. The id-derived fallback covers any unnumbered row
    // (there should be none) so two such rows can never share the "SI-0000" stem.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? ReferenceFor(Number)
        : $"SI-{SiteInstructionId.PadRight(8, '0')[..8].ToUpperInvariant()}";

    public static string ReferenceFor(int number) => $"SI-{number:0000}";
}
